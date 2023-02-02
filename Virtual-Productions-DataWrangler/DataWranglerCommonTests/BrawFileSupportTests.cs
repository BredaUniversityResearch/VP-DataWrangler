using System.IO;
using DataWranglerCommon;
using DataWranglerCommon.BRAWSupport;
using Xunit;

namespace BlackmagicDeckLinkControlTest
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
			TimeCode timeCode = BRAWFileHelper.GetTimeCodeFromFile(file, 0);
		}
	}
}
