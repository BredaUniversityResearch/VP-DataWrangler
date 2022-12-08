using Microsoft.Extensions.Configuration;

namespace DataWranglerCommon
{
	public class ShotGridApiKeyProvider
	{
		public static readonly string ShotGridApiScriptName = "ShotGrid DataWrangler";
		public static readonly string? ShotGridApiScriptKey;

		static ShotGridApiKeyProvider()
		{
			IConfigurationRoot configRoot = new ConfigurationBuilder().AddUserSecrets(typeof(ShotGridApiKeyProvider).Assembly).Build();
			string? configuredClientSecret = configRoot.GetSection("ShotGridApiScriptKey").Value;
			if (configuredClientSecret == null)
			{
				configuredClientSecret = Environment.GetEnvironmentVariable("DATAWRANGLER_KEY");
			}

			ShotGridApiScriptKey = configuredClientSecret;
		}
	}
}
