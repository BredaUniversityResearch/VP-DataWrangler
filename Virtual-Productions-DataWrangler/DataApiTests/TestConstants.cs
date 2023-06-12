using Renci.SshNet;
using Renci.SshNet.Common;

namespace DataApiTests
{
	internal class TestConstants
	{
		public const string TargetHost = "cradlenas";
		public const string TargetUser = "nas";
		private const string DefaultDataStoreFtpKeyFilePath = "nas.pem";
		public static readonly PrivateKeyFile TargetKeyFile;

		public static readonly Guid TargetProjectId = Guid.Parse("51d152e3-ba01-4bff-8fd1-2ee62858c143");

		static TestConstants()
		{
			if (File.Exists(DefaultDataStoreFtpKeyFilePath))
			{
				TargetKeyFile = new PrivateKeyFile(DefaultDataStoreFtpKeyFilePath);
			}
			else
			{
				throw new FileNotFoundException($"Could not find required data store key file at {DefaultDataStoreFtpKeyFilePath}");
			}
		}

	}
}
