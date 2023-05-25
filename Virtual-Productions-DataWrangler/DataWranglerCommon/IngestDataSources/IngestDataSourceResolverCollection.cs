using System.Reflection;
using CommonLogging;

namespace DataWranglerCommon.IngestDataSources
{
	public class IngestDataSourceResolverCollection
	{
		private readonly List<IngestDataSourceResolver> m_dataSourceResolvers = new List<IngestDataSourceResolver>();
		public IReadOnlyList<IngestDataSourceResolver> DataSourceResolvers => m_dataSourceResolvers;

		public IngestDataSourceResolverCollection()
		{
			CreateAvailableResolvers();
		}

		private void CreateAvailableResolvers()
		{
			foreach (Type resolverType in IngestTypeHelper.FindTypesInheritingFrom(typeof(IngestDataSourceResolver)))
			{
				ConstructorInfo? constructor = resolverType.GetConstructor(Type.EmptyTypes);
				if (constructor != null)
				{
					IngestDataSourceResolver handler = (IngestDataSourceResolver) constructor.Invoke(null);
					m_dataSourceResolvers.Add(handler);

					Logger.LogVerbose("IngestResolverCollection", $"Created ingest resolver of type {resolverType}");
				}
				else
				{
					Logger.LogError("IngestResolverCollection", $"Tried to create ingest resolver of type {resolverType}, but no parameterless constructor was found.");
				}
			}
		}
	}
}
