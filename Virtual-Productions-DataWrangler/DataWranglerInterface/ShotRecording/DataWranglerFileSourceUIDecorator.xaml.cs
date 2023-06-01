using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AutoNotify;
using DataWranglerCommon.IngestDataSources;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for DataWranglerFileSourceUIDecorator.xaml
	/// </summary>
	public partial class DataWranglerFileSourceUIDecorator : UserControl
	{
		private class UIMetaData
		{
			private class DisplayedEditData
			{
				public MemberInfo TargetMemberInfo;
				public EDataEditFlags InstanceFlags;
				public EDataEditFlags TemplateFlags;

				public DisplayedEditData(MemberInfo a_targetMemberInfo, EDataEditFlags a_instanceFlags, EDataEditFlags a_templateFlags)
				{
					TargetMemberInfo = a_targetMemberInfo;
					InstanceFlags = a_instanceFlags;
					TemplateFlags = a_templateFlags;
				}
			};

			private List<DisplayedEditData> m_fields = new List<DisplayedEditData>();

			public UIMetaData(Type a_type)
			{
				foreach (PropertyInfo propertyInfo in a_type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					IngestDataEditableAttribute? attributeData = propertyInfo.GetCustomAttribute<IngestDataEditableAttribute>(false);
					if (attributeData != null)
					{
						TryAddMemberFromInfo(propertyInfo, attributeData);
					}
					else
					{
						AutoNotifyPropertyAttribute? autoNotifyProperty = propertyInfo.GetCustomAttribute<AutoNotifyPropertyAttribute>(false);
						if (autoNotifyProperty != null)
						{
							FieldInfo? fi = a_type.GetField(autoNotifyProperty.BackingFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
							if (fi != null)
							{
								IngestDataEditableAttribute? ingestData = fi.GetCustomAttribute<IngestDataEditableAttribute>(false);
								if (ingestData != null)
								{
									TryAddMemberFromInfo(propertyInfo, ingestData);
								}
							}
						}
					}
				}
			}

			private void TryAddMemberFromInfo(MemberInfo a_info, IngestDataEditableAttribute a_attributeData)
			{
				if ((a_attributeData.Instance & EDataEditFlags.Visible) == EDataEditFlags.Visible ||
				    (a_attributeData.Template & EDataEditFlags.Visible) == EDataEditFlags.Visible)
				{
					m_fields.Add(new DisplayedEditData(a_info, a_attributeData.Instance, a_attributeData.Template));
				}
			}

			public void CreateEditControls(IngestDataSourceMeta a_target, Grid a_targetCollection)
			{
				int dataRow = 0;
				foreach (DisplayedEditData field in m_fields)
				{
					EDataEditFlags editFlags = field.InstanceFlags;
					if ((editFlags & EDataEditFlags.Visible) == 0)
					{
						continue;
					}
					a_targetCollection.RowDefinitions.Add(new RowDefinition());

					Label label = new Label
					{
						Content = field.TargetMemberInfo.Name
					};
					a_targetCollection.Children.Add(label);
					Grid.SetRow(label, dataRow);
					Grid.SetColumn(label, 0);

					Binding textBinding = new Binding(field.TargetMemberInfo.Name)
					{
						Source = a_target,
						Mode = ((editFlags & EDataEditFlags.Editable) == EDataEditFlags.Editable)? BindingMode.TwoWay : BindingMode.OneWay
					};
					TextBox box = new TextBox();
					box.SetBinding(TextBox.TextProperty, textBinding);
					a_targetCollection.Children.Add(box);

					Grid.SetRow(box, dataRow);
					Grid.SetColumn(box, 1);

					++dataRow;
				}
			}
		};

		private static readonly Dictionary<Type, UIMetaData> MetaForTypes = new();

		static DataWranglerFileSourceUIDecorator()
		{
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (Type type in assembly.GetTypes())
				{
					if (type.IsAssignableTo(typeof(IngestDataSourceMeta)))
					{
						MetaForTypes.Add(type, new UIMetaData(type));
					}
				}
			}
		}

		public DataWranglerFileSourceUIDecorator(IngestDataSourceMeta a_meta, Action? a_onRemoveAction)
		{
			InitializeComponent();

			FileSourceMeta.Content = a_meta.SourceType;

			if (MetaForTypes.TryGetValue(a_meta.GetType(), out UIMetaData? targetMeta))
			{
				targetMeta.CreateEditControls(a_meta, ContentContainer);
			}
			else
			{
				throw new Exception($"Unknown meta type of {a_meta.GetType()}");
			}

			if (a_onRemoveAction != null)
			{
				RemoveMetaButton.Click += (_, _) => { a_onRemoveAction.Invoke(); };
			}
			else
			{
				RemoveMetaButton.Visibility = Visibility.Hidden;
			}
		}
	}
}
