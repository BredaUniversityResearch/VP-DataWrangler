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
            Task<bool> startResult = service.AsyncStartCapture(CaptureName + DateTimeOffset.UtcNow.ToUnixTimeSeconds(), CaptureDatabasePath);
            startResult.Wait();
            Assert.True(startResult.Result);
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
