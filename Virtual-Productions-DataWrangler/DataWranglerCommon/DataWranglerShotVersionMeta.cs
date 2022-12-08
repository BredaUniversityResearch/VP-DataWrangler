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

		public TMetaType? FindFileSourceMeta<TMetaType>()
			where TMetaType : DataWranglerFileSourceMeta
		{
			foreach (DataWranglerFileSourceMeta meta in FileSources)
			{
				if (meta is TMetaType typedMeta)
				{
					return typedMeta;
				}
			}

			return null;
		}
	}
}