namespace DataWranglerCommon.IngestDataSources;

[Flags]
public enum EDataEditFlags
{
	None = 0,
	Visible = (1 << 0),
	Editable = (1 << 1) | Visible,
};

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class IngestDataEditableAttribute : Attribute
{
	public readonly EDataEditFlags Instance;
	public readonly EDataEditFlags Template;

	public IngestDataEditableAttribute(EDataEditFlags a_instance, EDataEditFlags a_template)
	{
		Instance = a_instance;
		Template = a_template;
	}
}