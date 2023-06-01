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
						if ((attributeData.Instance & EDataEditFlags.Visible) == EDataEditFlags.Visible ||
						    (attributeData.Template & EDataEditFlags.Visible) == EDataEditFlags.Visible)
						{
							MemberInfo targetInfo = propertyInfo;
							m_fields.Add(new DisplayedEditData(targetInfo, attributeData.Instance, attributeData.Template));
						}
					}
				}
			}

			public void CreateEditControls(IngestDataSourceMeta a_target, UIElementCollection a_targetCollection)
			{
				int dataRow = 0;
				foreach (DisplayedEditData field in m_fields)
				{
					Label label = new Label
					{
						Content = field.TargetMemberInfo.Name
					};
					a_targetCollection.Add(label);
					Grid.SetRow(label, dataRow);
					Grid.SetColumn(label, 0);

					Binding textBinding = new Binding(field.TargetMemberInfo.Name)
					{
						Source = a_target
					};
					TextBox box = new TextBox();
					box.SetBinding(TextBox.TextProperty, textBinding);
					a_targetCollection.Add(box);

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

			if (MetaForTypes.TryGetValue(a_meta.GetType(), out UIMetaData? targetMeta))
			{
				targetMeta.CreateEditControls(a_meta, ContentContainer.Children);
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
