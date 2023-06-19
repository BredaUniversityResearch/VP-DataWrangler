using CommonLogging;
using DataApiCommon;
using Newtonsoft.Json;

namespace DataApiSFTP;

internal class DataApiSFTPFilePublishAttributes
{
	[JsonProperty("entity_id")]
	public Guid EntityId = Guid.NewGuid();

	[JsonIgnore]
	public string PublishedFileName = "";

	[JsonIgnore]
	public string RelativePathToRoot = "";

	[JsonProperty("description")]
	public string Description = "";

	[JsonProperty("file_type_tag")]
	public string? FileTypeTag;

	[JsonProperty("storage_id")]
	public Guid StorageId = Guid.Empty;

	public DataApiSFTPFilePublishAttributes()
	{
	}

	public DataApiSFTPFilePublishAttributes(DataEntityFilePublish a_publish, DataEntityCache a_cache)
	{
		EntityId = a_publish.EntityId;
		Description = a_publish.Description;
		PublishedFileName = a_publish.PublishedFileName;
		RelativePathToRoot = a_publish.RelativePathToStorageRoot?? "";
		if (a_publish.PublishedFileType != null)
		{
			FileTypeTag = a_cache.FindEntityById<DataEntityPublishedFileType>(a_publish.PublishedFileType.EntityId)?.FileType ?? "";
		}

		if (a_publish.StorageRoot != null)
		{
			StorageId = a_publish.StorageRoot.EntityId;
		}
	}

	public DataEntityFilePublish ToDataEntity(DataEntityProject a_project, DataEntityShotVersion a_version, DataEntityCache a_cache)
	{
		var publish = new DataEntityFilePublish
		{
			EntityId = EntityId,
			Description = Description,
			EntityRelationships = {
				Parent = new DataEntityReference(a_version), 
				Project = new DataEntityReference(a_project)
			},
			Path = new DataEntityFileLink(),
			PublishedFileName = PublishedFileName,
			RelativePathToStorageRoot = RelativePathToRoot,
			ShotVersion = new DataEntityReference(a_version),
		};

		if (!string.IsNullOrEmpty(FileTypeTag))
		{
			DataEntityPublishedFileType? fileType = a_cache.FindEntityByPredicate<DataEntityPublishedFileType>((a_obj) => string.Equals(a_obj.FileType, FileTypeTag, StringComparison.OrdinalIgnoreCase));
			if (fileType != null)
			{
				publish.PublishedFileType = new DataEntityReference(fileType);
			}
			else
			{
				Logger.LogError("DataApiSFTP", $"Failed to find published file type with tag \"{FileTypeTag}\"");
			}
		}

		DataEntityLocalStorage? storage = a_cache.FindEntityById<DataEntityLocalStorage>(StorageId);
		if (storage != null)
		{
			publish.StorageRoot = new DataEntityReference(storage);
		}
		else
		{
			Logger.LogError("DataApiSFTP", $"Failed to find LocalStorage with id {StorageId}");
		}

		return publish;
	}
}