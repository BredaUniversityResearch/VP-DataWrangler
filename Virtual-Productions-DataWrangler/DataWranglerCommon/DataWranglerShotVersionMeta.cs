namespace DataWranglerCommon
{
	public class DataWranglerShotVersionMeta
	{
		public List<DataWranglerFileSourceMeta> FileSources = new List<DataWranglerFileSourceMeta>();

		public DataWranglerShotVersionMeta Clone()
		{
			List<DataWranglerFileSourceMeta> clonedSources = new List<DataWranglerFileSourceMeta>();
			foreach(DataWranglerFileSourceMeta fs in FileSources)
			{
				clonedSources.Add(fs.Clone());
			}

			return new DataWranglerShotVersionMeta
			{
				FileSources = clonedSources
			};
		}

		public DataWranglerFileSourceMeta? HasFileSourceForFile(DateTimeOffset a_fileInfoCreationTimeUtc, string a_storageName, string a_codecName)
		{
			foreach (DataWranglerFileSourceMeta meta in FileSources)
			{
				if (meta.IsSourceFor(a_fileInfoCreationTimeUtc, a_storageName, a_codecName))
				{
					return meta;
				}
			}

			return null;
		}
	}
}