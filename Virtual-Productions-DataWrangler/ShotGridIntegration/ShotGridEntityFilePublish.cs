using DataApiCommon;
using Newtonsoft.Json;

namespace ShotGridIntegration
{
	public class ShotGridEntityFilePublish : ShotGridEntity
	{
		public class FilePublishAttributes
		{
			[JsonProperty("code")]
			public string PublishedFileName = "";
			[JsonProperty("path")]
			public ShotGridFileLink? Path;
            [JsonProperty("path_cache")]
            public string? PathCache;
            [JsonProperty("path_cache_storage")]
            public ShotGridEntityReference? PathCacheStorage;
			[JsonProperty("published_file_type")]
			public ShotGridEntityReference? PublishedFileType;
            [JsonProperty("version")]
			public ShotGridEntityReference? ShotVersion;
            [JsonProperty("description")]
			public string Description = "";
			[JsonProperty("sg_status_list")]
			public string Status = ShotGridStatusListEntry.WaitingToBegin;
		};

		[JsonProperty("attributes")]
		public FilePublishAttributes Attributes = new FilePublishAttributes();

		public ShotGridEntityFilePublish()
		{
		}

		public ShotGridEntityFilePublish(DataEntityFilePublish a_publish)
			: base(a_publish)
		{
			Attributes.Description = a_publish.Description;
			if (a_publish.Path != null)
			{
				Attributes.Path = new ShotGridFileLink(a_publish.Path);
			}

			Attributes.PathCache = a_publish.RelativePathToStorageRoot;
			if (a_publish.StorageRoot != null)
			{
				Attributes.PathCacheStorage = new ShotGridEntityReference(a_publish.StorageRoot);
			}

			if (a_publish.PublishedFileType != null)
			{
				Attributes.PublishedFileType = new ShotGridEntityReference(a_publish.PublishedFileType);
			}

			if (a_publish.ShotVersion != null)
			{
				Attributes.ShotVersion = new ShotGridEntityReference(a_publish.ShotVersion);
			}

			Attributes.PublishedFileName = a_publish.PublishedFileName;
		}

		public override DataEntityFilePublish ToDataEntity()
		{
			DataEntityFilePublish publish = new DataEntityFilePublish()
			{
				Description = Attributes.Description, 
				Path = Attributes.Path?.ToDataEntity(),
				RelativePathToStorageRoot = Attributes.PathCache,
				StorageRoot = Attributes.PathCacheStorage?.ToDataEntity(),
				PublishedFileName = Attributes.PublishedFileName,
				PublishedFileType = Attributes.PublishedFileType?.ToDataEntity(),
				ShotVersion = Attributes.ShotVersion?.ToDataEntity()
			};
			CopyToDataEntity(publish);
			return publish;
		}
	}
}

/*
         {'code': 'layout.v001.ma',
         'created_by': {'id': 40, 'name': 'John Smith', 'type': 'HumanUser'},
         'description': 'Initial layout composition.',
         'entity': {'id': 2, 'name': 'shot_010', 'type': 'Shot'},
         'id': 2,
         'published_file_type': {'id': 134, 'type': 'PublishedFileType'},
         'name': 'layout.ma',
         'path': {'content_type': None,
          'link_type': 'local',
          'local_path': '/studio/demo_project/sequences/Sequence-1/shot_010/Anm/publish/layout.v001.ma',
          'local_path_linux': '/studio/demo_project/sequences/Sequence-1/shot_010/Anm/publish/layout.v001.ma',
          'local_path_mac': '/studio/demo_project/sequences/Sequence-1/shot_010/Anm/publish/layout.v001.ma',
          'local_path_windows': 'c:\\studio\\demo_project\\sequences\\Sequence-1\\shot_010\\Anm\\publish\\layout.v001.ma',
          'local_storage': {'id': 1, 'name': 'primary', 'type': 'LocalStorage'},
          'name': 'layout.v001.ma',
          'url': 'file:///studio/demo_project/sequences/Sequence-1/shot_010/Anm/publish/layout.v001.ma'},
         'path_cache': 'demo_project/sequences/Sequence-1/shot_010/Anm/publish/layout.v001.ma',
         'project': {'id': 4, 'name': 'Demo Project', 'type': 'Project'},
         'published_file_type': {'id': 12, 'name': 'Layout Scene', 'type': 'PublishedFileType'},
         'task': None,
         'type': 'PublishedFile',
         'version_number': 1}
*/