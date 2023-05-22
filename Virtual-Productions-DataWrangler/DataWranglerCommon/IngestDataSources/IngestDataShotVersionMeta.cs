namespace DataWranglerCommon.IngestDataSources
{
    public class IngestDataShotVersionMeta
    {
        public List<IngestDataSourceMeta> FileSources = new List<IngestDataSourceMeta>();

        public IngestDataShotVersionMeta Clone()
        {
            List<IngestDataSourceMeta> clonedSources = new List<IngestDataSourceMeta>();
            foreach (IngestDataSourceMeta fs in FileSources)
            {
                clonedSources.Add(fs.Clone());
            }

            return new IngestDataShotVersionMeta
            {
                FileSources = clonedSources
            };
        }

        public TMetaType? FindMetaByType<TMetaType>()
            where TMetaType : IngestDataSourceMeta
        {
            foreach (IngestDataSourceMeta meta in FileSources)
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