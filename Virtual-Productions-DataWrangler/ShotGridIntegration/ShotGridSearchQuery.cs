﻿using System.Diagnostics.CodeAnalysis;

namespace ShotGridIntegration
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class ShotGridSearchQuery
	{
		public ShotGridSimpleSearchFilter filters;
		public string[] fields;
		public ShotGridSortSpecifier? sort;

		public ShotGridSearchQuery(ShotGridSimpleSearchFilter a_filters, string[] a_fields, ShotGridSortSpecifier? a_sort)
		{
			filters = a_filters;
			fields = a_fields;
			sort = a_sort;
		}
	}
}