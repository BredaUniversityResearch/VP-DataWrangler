using Newtonsoft.Json;

namespace ShotGridIntegration
{
	public class ShotGridEntityLocalStorage : ShotGridEntity
	{
		public class LocalStorageAttributes
		{
			[JsonProperty("code")]
			public string LocalStorageName = "";
			[JsonProperty("mac_path")]
			public string? MacPath;
			[JsonProperty("windows_path")]
			public string WindowsPath = "";
			[JsonProperty("linux_path")]
			public string? LinuxPath;
		};

		[JsonProperty("attributes")]
		public LocalStorageAttributes Attributes = new LocalStorageAttributes();
	}
}
