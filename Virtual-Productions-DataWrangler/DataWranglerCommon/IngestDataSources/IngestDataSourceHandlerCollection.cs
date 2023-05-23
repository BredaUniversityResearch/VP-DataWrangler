using System.Reflection;
using CommonLogging;

namespace DataWranglerCommon.IngestDataSources
{
	public class IngestDataSourceHandlerCollection
	{
		private readonly List<IngestDataSourceHandler> m_dataSourceHandlers = new List<IngestDataSourceHandler>();

		public void CreateAvailableHandlers(DataWranglerEventDelegates a_eventDelegates, DataWranglerServices a_services)
		{
			foreach (Type handlerType in FindAvailableHandlerTypes())
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

		private List<Type> FindAvailableHandlerTypes()
		{
			List<Type> resultTypes = new();
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (Type type in assembly.GetTypes())
				{
					if (!type.IsAbstract && type.IsAssignableTo(typeof(IngestDataSourceHandler)))
					{
						resultTypes.Add(type);
					}
				}
			}

			return resultTypes;
		}
	}
}
