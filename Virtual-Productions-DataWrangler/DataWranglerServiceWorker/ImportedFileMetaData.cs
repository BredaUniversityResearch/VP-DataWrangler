
using System;
using Newtonsoft.Json;
using ShotGridIntegration;

namespace DataWranglerServiceWorker
{
	//Data that should be written out next to a file that has been imported.
	public class ImportedFileMetaData
	{
		[JsonProperty("project_id")]
		public readonly int ProjectId;
		[JsonProperty("shot_id")]
		public readonly int ShotId;
		[JsonProperty("version_id")]
		public readonly int VersionId;

		[JsonProperty("project_code")]
		public readonly string ProjectCode = "Unknown";
		[JsonProperty("shot_code")]
		public readonly string ShotCode = "Unknown";
		[JsonProperty("version_code")]
		public readonly string VersionCode = "Unknown";

		[JsonProperty("imported_finished")]
		public readonly DateTime ImportFinishedTime = DateTime.UtcNow;

		public ImportedFileMetaData(ShotGridEntityShotVersion a_targetShotVersion, ShotGridDataCache a_cache)
		{
			ProjectId = a_targetShotVersion.EntityRelationships.Project?.Id?? -1;
			ShotId = a_targetShotVersion.EntityRelationships.Parent?.Id?? -1;
			VersionId = a_targetShotVersion.Id;

			if (a_cache.FindEntityById(ProjectId, out ShotGridEntityProject? project))
			{
				ShotCode = project.Attributes.Name;
			}

			if (a_cache.FindEntityById(ShotId, out ShotGridEntityShot? shot))
			{
				ShotCode = shot.Attributes.ShotCode;
			}

			if (a_cache.FindEntityById(VersionId, out ShotGridEntityShotVersion? version))
			{
				VersionCode = version.Attributes.VersionCode;
			}

		}
	}
}
