using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataWranglerCommon.IngestDataSources
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class IngestDataSourceMetaAttribute: Attribute
	{
		public readonly Type HandlerType;
		public readonly Type MetaResolverType;

		public IngestDataSourceMetaAttribute(Type a_handlerType, Type a_metaResolverType)
		{
			HandlerType = a_handlerType;
			if (HandlerType.IsAssignableTo(typeof(IngestDataSourceHandler)) == false)
			{
				throw new Exception("Ingest handler type must inherit from IngestDataSourceHandler");
			}

			MetaResolverType = a_metaResolverType;
			if (MetaResolverType.IsAssignableTo(typeof(IngestDataSourceResolver)) == false)
			{
				throw new Exception("Ingest handler type must inherit from IngestDataSourceResolver");
			}
		}
	}
}
