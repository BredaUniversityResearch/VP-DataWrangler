using System.ComponentModel;

namespace DataApiCommon
{
	public abstract class IngestDataSourceMeta: INotifyPropertyChanged
	{
		public abstract string SourceType { get; }
		public abstract IngestDataSourceMeta Clone();

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnAutoPropertyChanged(string a_propertyName) 
		{ 
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(a_propertyName)); 
		}
	}
}
