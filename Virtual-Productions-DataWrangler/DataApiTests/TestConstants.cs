using Renci.SshNet;
using Renci.SshNet.Common;

namespace DataApiTests
{
	internal class TestConstants
	{
		public const string TargetHost = "cradlenas";
		public const string TargetUser = "nas"; 
		public const string DefaultDataStoreFtpKeyFilePath = "nas.pem";

		public static readonly Guid TargetProjectId = Guid.Parse("51d152e3-ba01-4bff-8fd1-2ee62858c143");
		public static readonly Guid TargetShotId = Guid.Parse("fbc076eb-c9a7-4c08-b0c4-a52ee650d6e4");
		public static readonly Guid TargetShotVersionId = Guid.Parse("1a20f7d6-34a4-4055-99e5-f5e8068a0000");
	}
}
