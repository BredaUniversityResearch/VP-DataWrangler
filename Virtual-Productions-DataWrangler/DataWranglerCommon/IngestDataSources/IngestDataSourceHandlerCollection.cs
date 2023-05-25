using System.Reflection;
using CommonLogging;

namespace DataWranglerCommon.IngestDataSources
{
	public class IngestDataSourceHandlerCollection
	{
		private readonly List<IngestDataSourceHandler> m_dataSourceHandlers = new List<IngestDataSourceHandler>();

		public void CreateAvailableHandlers(DataWranglerEventDelegates a_eventDelegates, DataWranglerServices a_services)
		{
			foreach (Type handlerType in IngestTypeHelper.FindTypesInheritingFrom(typeof(IngestDataSourceHandler)))
			{
				ConstructorInfo? constructor = handlerType.GetConstructor(Type.EmptyTypes);
				if (constructor != null)
				{
					IngestDataSourceHandler handler = (IngestDataSourceHandler) constructor.Invoke(null);
					handler.InstallHooks(a_eventDelegates, a_services);
					m_dataSourceHandlers.Add(handler);

					Logger.LogVerbose("IngestHandlerCollection", $"Created ingest handler of type {handlerType}");
				}
				else
				{
					Logger.LogError("IngestHandlerCollection", $"Tried to create ingest handler of type {handlerType}, but no parameterless constructor was found.");
				}
			}
		}
	}
}
