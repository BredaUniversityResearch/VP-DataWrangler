
using System;
using DataWranglerCommon.IngestDataSources;
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

		public ImportedFileMetaData(ShotGridEntityShotVersion a_targetShotVersion, ShotGridEntityCache a_dataCache)
		{
			ProjectId = a_targetShotVersion.EntityRelationships.Project?.Id?? -1;
			ShotId = a_targetShotVersion.EntityRelationships.Parent?.Id?? -1;
			VersionId = a_targetShotVersion.Id;

			ShotGridEntityProject? project = a_dataCache.FindEntityById<ShotGridEntityProject>(ProjectId);
			if (project != null)
			{
				ShotCode = project.Attributes.Name;
			}

			ShotGridEntityShot? shot = a_dataCache.FindEntityById<ShotGridEntityShot>(ShotId);
			if (shot != null)
			{
				ShotCode = shot.Attributes.ShotCode;
			}

			ShotGridEntityShotVersion? shotVersion = a_dataCache.FindEntityById<ShotGridEntityShotVersion>(VersionId);
			if (shotVersion != null)
			{
				VersionCode = shotVersion.Attributes.VersionCode;
			}

		}
	}
}
