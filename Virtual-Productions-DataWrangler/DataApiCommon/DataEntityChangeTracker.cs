using System.ComponentModel;
using System.Reflection;
using AutoNotify;
using Newtonsoft.Json;

namespace DataApiCommon
{
	public class DataEntityChangeTracker
	{
		private readonly DataEntityBase m_targetEntity;
		private readonly Dictionary<FieldInfo, object> m_changedFields = new();

		public DataEntityChangeTracker(DataEntityBase a_targetEntity)
		{
			m_targetEntity = a_targetEntity;

			Type targetType = a_targetEntity.GetType();
			foreach (FieldInfo info in targetType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (info.GetValue(a_targetEntity) is INotifyPropertyChanged propertyChanged)
				{
					propertyChanged.PropertyChanged += OnChildPropertyChanged;
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

			AutoNotifyPropertyAttribute? propertyAttribute = prop.GetCustomAttribute<AutoNotifyPropertyAttribute>(true);
			if (propertyAttribute == null)
			{
				throw new Exception($"Backing field reference property not found on property {a_e.PropertyName}");
			}

			FieldInfo? backingField = a_sender.GetType().GetField(propertyAttribute.BackingFieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			if (backingField == null)
			{
				throw new Exception($"Backing field with name {propertyAttribute.BackingFieldName} not found");
			}

			object? newValue = backingField.GetValue(a_sender);
			if (newValue == null)
			{
				throw new Exception("Failed to get value from backing field");
			}

			m_changedFields[backingField] = newValue;
		}

		public Task<DataApiResponseGeneric> CommitChanges(DataApi a_targetApi)
		{
			Dictionary<string, object> changedValues = new Dictionary<string, object>();
			foreach (KeyValuePair<FieldInfo, object> kvp in m_changedFields)
			{
				string fieldName = kvp.Key.Name;
				JsonPropertyAttribute? nameAttribute = kvp.Key.GetCustomAttribute<JsonPropertyAttribute>();
				if (nameAttribute != null && nameAttribute.PropertyName != null)
				{
					fieldName = nameAttribute.PropertyName;
				}

				changedValues.Add(fieldName, kvp.Value);
			}

			m_changedFields.Clear();

			return Task.Run(() => a_targetApi.UpdateEntityProperties(m_targetEntity, changedValues));
		}

		public bool HasAnyUncommittedChanges()
		{
			return m_changedFields.Count > 0;
		}
	}
}
