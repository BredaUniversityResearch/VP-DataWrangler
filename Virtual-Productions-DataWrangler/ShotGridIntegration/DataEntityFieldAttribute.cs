namespace ShotGridIntegration
{
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class DataEntityFieldAttribute: Attribute
	{
		public readonly string DataEntityFieldName;

		public DataEntityFieldAttribute(string a_dataEntityFieldName)
		{
			DataEntityFieldName = a_dataEntityFieldName;
		}
	}
}
