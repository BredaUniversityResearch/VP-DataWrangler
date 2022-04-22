using System.Diagnostics.CodeAnalysis;

namespace ShotGridIntegration
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class ShotGridSearchQuery
	{
		public ShotGridSimpleSearchFilter filters;
		public string[] fields;

		public ShotGridSearchQuery(ShotGridSimpleSearchFilter a_filters, string[] a_fields)
		{
			filters = a_filters;
			fields = a_fields;
		}
	}
}