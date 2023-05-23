using DataWranglerCommon.ShogunLiveSupport;
using ShotGridIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataWranglerCommon
{
	public class DataWranglerServices
	{
		public readonly ShotGridAPI ShotGridAPI;
		public readonly ShogunLiveService ShogunLiveService;

		public DataWranglerServices(ShotGridAPI a_shotGridAPI, ShogunLiveService a_shogunLiveService)
		{
			ShotGridAPI = a_shotGridAPI;
			ShogunLiveService = a_shogunLiveService;
		}
	}
}
