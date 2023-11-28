using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DataApiCommon
{
	public class IngestDataShotVersionMeta : INotifyPropertyChanged
	{
		public List<IngestDataSourceMeta> FileSources;

		public IngestDataShotVersionMeta()
		{
			FileSources = new();
		}

		public IngestDataShotVersionMeta(List<IngestDataSourceMeta> a_fileSources)
		{
			FileSources = a_fileSources;
		}

		public IngestDataShotVersionMeta Clone()
		{
			List<IngestDataSourceMeta> clonedSources = new List<IngestDataSourceMeta>();
			foreach (IngestDataSourceMeta fs in FileSources)
			{
				clonedSources.Add(fs.Clone());
			}

			return new IngestDataShotVersionMeta(clonedSources);
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

		public IngestDataSourceMeta? FindMetaByType(string a_metaTypeName)
		{
			return FileSources.Find(a_source => a_source.SourceType == a_metaTypeName);
		}

		public bool Contains(IngestDataSourceMeta a_meta)
		{
			return FileSources.Contains(a_meta);
		}

		public void AddFileSource(IngestDataSourceMeta a_meta)
		{
			FileSources.Add(a_meta);
			OnPropertyChanged(nameof(FileSources));
		}

		public bool RemoveFileSourceInstance(IngestDataSourceMeta a_instance)
		{
			bool removed = FileSources.Remove(a_instance);
			if (removed)
			{
				OnPropertyChanged(nameof(FileSources));
			}

			return removed;
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected virtual void OnPropertyChanged([CallerMemberName] string? a_propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(a_propertyName));
		}
	}
}