
using System;
using DataApiCommon;
using DataWranglerCommon.IngestDataSources;
using Newtonsoft.Json;
using ShotGridIntegration;

namespace DataWranglerServiceWorker
{
	//Data that should be written out next to a file that has been imported.
	public class ImportedFileMetaData
	{
		[JsonProperty("project_id")]
		public readonly Guid ProjectId;
		[JsonProperty("shot_id")]
		public readonly Guid ShotId;
		[JsonProperty("version_id")]
		public readonly Guid VersionId;

		[JsonProperty("project_code")]
		public readonly string ProjectName = "Unknown";
		[JsonProperty("shot_code")]
		public readonly string ShotName = "Unknown";
		[JsonProperty("version_code")]
		public readonly string VersionName = "Unknown";

		[JsonProperty("imported_finished")]
		public readonly DateTime ImportFinishedTime = DateTime.UtcNow;

		public ImportedFileMetaData(DataEntityShotVersion a_targetShotVersion, DataEntityCache a_dataCache)
		{
			ProjectId = a_targetShotVersion.EntityRelationships.Project?.EntityId?? Guid.Empty;
			ShotId = a_targetShotVersion.EntityRelationships.Parent?.EntityId ?? Guid.Empty;
			VersionId = a_targetShotVersion.EntityId;

			DataEntityProject? project = a_dataCache.FindEntityById<DataEntityProject>(ProjectId);
			if (project != null)
			{
				ProjectName = project.Name;
			}

			DataEntityShot? shot = a_dataCache.FindEntityById<DataEntityShot>(ShotId);
			if (shot != null)
			{
				ShotName = shot.ShotName;
			}

			DataEntityShotVersion? shotVersion = a_dataCache.FindEntityById<DataEntityShotVersion>(VersionId);
			if (shotVersion != null)
			{
				VersionName = shotVersion.ShotVersionName;
			}

		}
	}
}
