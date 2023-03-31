using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataWranglerCommon.ShogunLiveSupport;

namespace DataWranglerCommonTests
{
    public class ShogunLiveTests
    {
        private const string CaptureName = "UNIT_TEST";
        private const string CaptureDatabasePath = "C:/Temp/ViconTempDb/UNIT_TEST_DB";

        [Fact]
        public void StartCapture()
        {
            using ShogunLiveService service = new ShogunLiveService(30);
            bool startResult = service.StartCapture(CaptureName + DateTimeOffset.UtcNow.ToUnixTimeSeconds(), CaptureDatabasePath, out Task<bool>? confirmationPromise);
            Assert.True(startResult);
            Assert.True(confirmationPromise!.Result);
        }

        [Fact]
        public void StopCapture()
        {
            using ShogunLiveService service = new ShogunLiveService(30);
            bool stopResult = service.StopCapture(true, CaptureName + DateTimeOffset.UtcNow.ToUnixTimeSeconds(), CaptureDatabasePath);
            Assert.True(stopResult);
        }
	}
}
