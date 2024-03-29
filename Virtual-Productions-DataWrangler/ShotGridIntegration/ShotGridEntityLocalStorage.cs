﻿using DataApiCommon;
using Newtonsoft.Json;

namespace ShotGridIntegration
{
	internal class ShotGridEntityLocalStorage : ShotGridEntity
	{
		public class LocalStorageAttributes
		{
			[JsonProperty("code")]
			public string LocalStorageName = "";
			[JsonProperty("mac_path")]
			public string? MacPath = null;
			[JsonProperty("windows_path")]
			public string WindowsPath = "";
			[JsonProperty("linux_path")]
			public string? LinuxPath = null;
		};

		[JsonProperty("attributes")]
		public LocalStorageAttributes Attributes = new LocalStorageAttributes();

		protected override DataEntityBase ToDataEntityInternal()
		{
			return new DataEntityLocalStorage() {LocalStorageName = Attributes.LocalStorageName, StorageRoot = new Uri(Attributes.WindowsPath)};
		}
	}
}
