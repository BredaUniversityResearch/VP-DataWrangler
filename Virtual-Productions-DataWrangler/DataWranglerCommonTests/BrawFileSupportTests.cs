using BlackmagicCameraControlData;
using DataWranglerCommon;
using DataWranglerCommon.BRAWSupport;

namespace DataWranglerCommonTests
{
	public class BrawFileSupportTests
	{
		private const string TestFile = "../../../../ThirdParty/Blackmagic RAW SDK/Sample/sample.braw";

		[Fact]
		public void CheckFileExists()
		{
			FileInfo file = new FileInfo(TestFile);
			Assert.True(file.Exists, $"Expected test file to exist at {file.FullName}");
		}

		[Fact]
		public void ExtractTimeCodeFromFrame()
		{
			FileInfo file = new FileInfo(TestFile);
			Task t = new Task(() =>
			{
				using (BRAWFileDecoder decoder = new BRAWFileDecoder())
				{
					TimeCode timeCode = decoder.GetTimeCodeFromFile(file, 0);
					Assert.True(timeCode == new TimeCode(22, 23, 40, 20));
				}
			});
			t.Start();
			Assert.True(t.Wait(new TimeSpan(0, 0, 3)));
		}

		[Fact]
		public void DecoderReUse()
		{
			FileInfo file = new FileInfo(TestFile);
			using (BRAWFileDecoder decoder = new BRAWFileDecoder())
			{
				TimeCode timeCode = decoder.GetTimeCodeFromFile(file, 0);
				Assert.True(timeCode == new TimeCode(22, 23, 40, 20));
				timeCode = decoder.GetTimeCodeFromFile(file, 0);
				Assert.True(timeCode == new TimeCode(22, 23, 40, 20));
				timeCode = decoder.GetTimeCodeFromFile(file, 0);
				Assert.True(timeCode == new TimeCode(22, 23, 40, 20));
			}
		}

		[Fact]
		public void DecodeMeta()
		{
			FileInfo file = new FileInfo(TestFile);
			using (BRAWFileDecoder decoder = new BRAWFileDecoder())
			{
				BRAWFileMetadata fileMeta = decoder.TryDecodeMetaForFile(file)!;
				Assert.True(fileMeta.FirstFrameTimeCode == new TimeCode(22, 23, 40, 20));
				Assert.True(fileMeta.CameraNumber == "A");
			}
		}
	}
}
