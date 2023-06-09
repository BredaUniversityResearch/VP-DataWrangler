using DataApiCommon;
using DataWranglerCommon.ShogunLiveSupport;

namespace DataWranglerCommon
{
	public class DataWranglerServices
	{
		public readonly DataApi TargetDataApi;
		public readonly ShogunLiveService ShogunLiveService;

		public DataWranglerServices(DataApi a_targetDataApi, ShogunLiveService a_shogunLiveService)
		{
			TargetDataApi = a_targetDataApi;
			ShogunLiveService = a_shogunLiveService;
		}
	}
}
