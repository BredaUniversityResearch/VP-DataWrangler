using CameraControlOverEthernet;
using DataApiCommon;
using DataWranglerCommon.ShogunLiveSupport;

namespace DataWranglerCommon
{
	public class DataWranglerServices
	{
		public readonly DataApi TargetDataApi;
		public readonly ShogunLiveService ShogunLiveService;
		public readonly NetworkedDeviceAPIServer NetworkDeviceAPI;

		public DataWranglerServices(DataApi a_targetDataApi, ShogunLiveService a_shogunLiveService, NetworkedDeviceAPIServer a_deviceNetworkDeviceAPI)
		{
			TargetDataApi = a_targetDataApi;
			ShogunLiveService = a_shogunLiveService;
			NetworkDeviceAPI = a_deviceNetworkDeviceAPI;
		}
	}
}
