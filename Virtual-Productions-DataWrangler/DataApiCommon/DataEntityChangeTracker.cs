using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using CommonLogging;

namespace DataApiCommon
{
	public class DataEntityChangeTracker
	{
		public event PropertyChangedEventHandler? OnChangeApplied = null;

		private readonly DataEntityBase m_targetEntity;
		private readonly Dictionary<PropertyInfo, object?> m_changedFields = new();
		private readonly Dictionary<PropertyInfo, KeyValuePair<INotifyPropertyChanged, PropertyChangedEventHandler>> m_propertyChangedByChildProperty = new();

		public DataEntityChangeTracker(DataEntityBase a_targetEntity)
		{
			m_targetEntity = a_targetEntity;

			Type targetType = a_targetEntity.GetType();
			a_targetEntity.PropertyChanged += OnChildPropertyChanged;
			foreach (PropertyInfo info in targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (info.GetValue(a_targetEntity) is INotifyPropertyChanged propertyChanged)
				{
					//Flatten any changes in the child hierarchy
					PropertyChangedEventHandler eventHandler = (_, _) => OnChildPropertyChanged(a_targetEntity, new PropertyChangedEventArgs(info.Name));
					propertyChanged.PropertyChanged += eventHandler;
					m_propertyChangedByChildProperty[info] = new KeyValuePair<INotifyPropertyChanged, PropertyChangedEventHandler>(propertyChanged, eventHandler);
				}
			}
		}

		private void OnChildPropertyChanged(object? a_sender, PropertyChangedEventArgs a_e)
		{
			if (a_sender == null || string.IsNullOrEmpty(a_e.PropertyName))
			{
				throw new NullReferenceException(nameof(a_sender));
			}

			PropertyInfo? prop = a_sender.GetType().GetProperty(a_e.PropertyName);
			if (prop == null)
			{
				throw new Exception($"Property with name {a_e.PropertyName} not found");
			}

			object? newValue = prop.GetValue(a_sender);

			//Update event handler so that we preserve the nice notifications from direct children.
			if (m_propertyChangedByChildProperty.TryGetValue(prop, out KeyValuePair<INotifyPropertyChanged, PropertyChangedEventHandler> existingHandler))
			{
				existingHandler.Key.PropertyChanged -= existingHandler.Value;
			}
			m_propertyChangedByChildProperty.Remove(prop);

			if (newValue is INotifyPropertyChanged propertyChangedHandler)
			{
				PropertyChangedEventHandler handler = (_, _) => OnChildPropertyChanged(m_targetEntity, new PropertyChangedEventArgs(prop.Name));
				m_propertyChangedByChildProperty[prop] = new KeyValuePair<INotifyPropertyChanged, PropertyChangedEventHandler>(propertyChangedHandler, handler);
				propertyChangedHandler.PropertyChanged += handler;
			}

			m_changedFields[prop] = newValue;

			OnChangeApplied?.Invoke(a_sender, a_e);
		}

		public Task<DataApiResponseGeneric> CommitChanges(DataApi a_targetApi)
		{
			if (m_changedFields.Count == 0)
			{
				Logger.LogError("ChangeTracker", "Trying to commit changes without any changes. Could this be a double commit?");
			}

			Dictionary<PropertyInfo, object?> changedValues = new Dictionary<PropertyInfo, object?>(m_changedFields);
			m_changedFields.Clear();

			Logger.LogVerbose("ChangeTracker", $"Committing Changes for {m_targetEntity}.");

			return a_targetApi.UpdateEntityProperties(m_targetEntity, changedValues);
		}

		public bool HasAnyUncommittedChanges()
		{
			return m_changedFields.Count > 0;
		}

		public void ClearChangedState()
		{
			m_changedFields.Clear();
		}
	}
}
