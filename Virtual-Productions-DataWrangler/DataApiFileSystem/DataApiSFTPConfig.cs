using Renci.SshNet.Common;
using Renci.SshNet;
using CommonLogging;

namespace DataApiSFTP
{
	public class DataApiSFTPConfig
	{
		public string TargetHost = "cradlenas";    //Host for the sFTP file publisher.
		public string SFTPUserName = "nas";
		public string SFTPRelativeRoot = "/projects/VirtualProductions/"; //What folder in sFTP should we publish to? 
		public string SFTPKeyFilePath = "nas.pem";

		public PrivateKeyFile? SFTPKeyFile;

		public static readonly DataApiSFTPConfig DefaultConfig = new DataApiSFTPConfig();

		private DataApiSFTPConfig()
		{
			if (!TryReloadPrivateKey())
			{
				Logger.LogError("Config", $"Failed to acquire ftp private key, file \"{SFTPKeyFilePath}\" not found.");
			}
		}

		public bool TryReloadPrivateKey()
		{
			if (File.Exists(SFTPKeyFilePath))
			{
				try
				{
					SFTPKeyFile = new PrivateKeyFile(SFTPKeyFilePath);
				}
				catch (SshException ex)
				{
					Logger.LogError("Config", $"Failed to load private key file. Exception: {ex.Message}");
				}

				return SFTPKeyFile != null;
			}

			return false;
		}
	}
}
