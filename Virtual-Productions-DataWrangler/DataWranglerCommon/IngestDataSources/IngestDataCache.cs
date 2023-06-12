using CommonLogging;
using DataApiCommon;
using Newtonsoft.Json;

namespace DataWranglerCommon.IngestDataSources
{
	public class IngestDataCache
	{
		class CacheEntry
		{
			public DataEntityShotVersion ShotVersion;
			public IngestDataShotVersionMeta DecodedMeta;

			public CacheEntry(DataEntityShotVersion a_shotVersion, IngestDataShotVersionMeta a_decodedMeta)
			{
				ShotVersion = a_shotVersion;
				DecodedMeta = a_decodedMeta;
			}
		};

		private readonly Dictionary<Guid, CacheEntry> m_cachedEntriesByShotId = new Dictionary<Guid, CacheEntry>();

		public void UpdateCache(DataEntityCache a_cache)
		{
			DataEntityShotVersion[] shotVersions = a_cache.GetEntitiesByType<DataEntityShotVersion>();
			foreach (DataEntityShotVersion version in shotVersions)
			{
				if (version.DataWranglerMeta != null)
				{
					try
					{
						IngestDataShotVersionMeta? decodedMeta = JsonConvert.DeserializeObject<IngestDataShotVersionMeta>(version.DataWranglerMeta, DataWranglerSerializationSettings.Instance);
						if (decodedMeta != null)
						{
							Logger.LogInfo("MetaCache", $"Got valid meta for shot version {version.EntityId}");

							AddOrUpdateMeta(version, decodedMeta);
						}
					}
					catch (JsonReaderException ex)
					{
						Logger.LogError("MetaCache",
							$"Failed to read json data for shot version {version.EntityRelationships.Project?.EntityName}/{version.EntityRelationships.Parent?.EntityName} ({version.EntityId}). Exception: {ex.Message}");
					}
					catch (JsonSerializationException ex)
					{
						Logger.LogError("MetaCache", $"Failed to deserialize data for shot version {version.EntityRelationships.Project?.EntityName}/{version.EntityRelationships.Parent?.EntityName} ({version.EntityId}). Exception: {ex.Message}");
					}
				}
			}
		}

		private void AddOrUpdateMeta(DataEntityShotVersion a_shotVersion, IngestDataShotVersionMeta a_decodedMeta)
		{
			if (m_cachedEntriesByShotId.TryGetValue(a_shotVersion.EntityId, out CacheEntry? entry))
			{
				entry.DecodedMeta = a_decodedMeta;
			}
			else
			{
				m_cachedEntriesByShotId.Add(a_shotVersion.EntityId, new CacheEntry(a_shotVersion, a_decodedMeta));
			}
		}

		public List<KeyValuePair<DataEntityShotVersion, TMetaType>> FindShotVersionsWithMeta<TMetaType>()
			where TMetaType: IngestDataSourceMeta
		{
			List<KeyValuePair<DataEntityShotVersion, TMetaType>> result = new();
			foreach (CacheEntry entry in m_cachedEntriesByShotId.Values)
			{
				TMetaType? meta = entry.DecodedMeta.FindMetaByType<TMetaType>();
				if (meta != null)
				{
					result.Add(new KeyValuePair<DataEntityShotVersion, TMetaType>(entry.ShotVersion, meta));
				}
			}

			return result;
		}
	}
}
