using CommonLogging;
using Newtonsoft.Json;
using ShotGridIntegration;

namespace DataWranglerCommon.IngestDataSources
{
	public class IngestDataCache
	{
		class CacheEntry
		{
			public ShotGridEntityShotVersion ShotVersion;
			public IngestDataShotVersionMeta DecodedMeta;

			public CacheEntry(ShotGridEntityShotVersion a_shotVersion, IngestDataShotVersionMeta a_decodedMeta)
			{
				ShotVersion = a_shotVersion;
				DecodedMeta = a_decodedMeta;
			}
		};

		private readonly Dictionary<int, CacheEntry> m_cachedEntriesByShotId = new Dictionary<int, CacheEntry>();

		public void UpdateCache(ShotGridEntityCache a_cache)
		{
			ShotGridEntityShotVersion[] shotVersions = a_cache.GetEntitiesByType<ShotGridEntityShotVersion>();
			foreach (ShotGridEntityShotVersion version in shotVersions)
			{
				if (version.Attributes.DataWranglerMeta != null)
				{
					try
					{
						IngestDataShotVersionMeta? decodedMeta = JsonConvert.DeserializeObject<IngestDataShotVersionMeta>(version.Attributes.DataWranglerMeta, DataWranglerSerializationSettings.Instance);
						if (decodedMeta != null)
						{
							Logger.LogInfo("MetaCache", $"Got valid meta for shot version {version.Id}");

							AddOrUpdateMeta(version, decodedMeta);
						}
					}
					catch (JsonReaderException ex)
					{
						Logger.LogError("MetaCache",
							$"Failed to read json data for shot version {version.EntityRelationships.Project?.EntityName}/{version.EntityRelationships.Parent?.EntityName} ({version.Id}). Exception: {ex.Message}");
					}
					catch (JsonSerializationException ex)
					{
						Logger.LogError("MetaCache", $"Failed to deserialize data for shot version {version.EntityRelationships.Project?.EntityName}/{version.EntityRelationships.Parent?.EntityName} ({version.Id}). Exception: {ex.Message}");
					}
				}
			}
		}

		private void AddOrUpdateMeta(ShotGridEntityShotVersion a_shotVersion, IngestDataShotVersionMeta a_decodedMeta)
		{
			if (m_cachedEntriesByShotId.TryGetValue(a_shotVersion.Id, out CacheEntry? entry))
			{
				entry.DecodedMeta = a_decodedMeta;
			}
			else
			{
				m_cachedEntriesByShotId.Add(a_shotVersion.Id, new CacheEntry(a_shotVersion, a_decodedMeta));
			}
		}

		public List<KeyValuePair<ShotGridEntityShotVersion, TMetaType>> FindShotVersionsWithMeta<TMetaType>()
			where TMetaType: IngestDataSourceMeta
		{
			List<KeyValuePair<ShotGridEntityShotVersion, TMetaType>> result = new();
			foreach (CacheEntry entry in m_cachedEntriesByShotId.Values)
			{
				TMetaType? meta = entry.DecodedMeta.FindMetaByType<TMetaType>();
				if (meta != null)
				{
					result.Add(new KeyValuePair<ShotGridEntityShotVersion, TMetaType>(entry.ShotVersion, meta));
				}
			}

			return result;
		}
	}
}
