using System.IO;
using Newtonsoft.Json.Linq;

namespace ShotGridIntegrationTest
{
	static class CredentialProvider
	{
		public static readonly string? Username;
		public static readonly string? Password;
		
		static CredentialProvider()
		{
			if (File.Exists("test_credentials.json"))
			{
				string contents = File.ReadAllText("test_credentials.json");
				JObject credentialsObject = JObject.Parse(contents);

				Username = credentialsObject["user"]?.ToString();
				Password = credentialsObject["pass"]?.ToString();
			}
		}
	}
}
