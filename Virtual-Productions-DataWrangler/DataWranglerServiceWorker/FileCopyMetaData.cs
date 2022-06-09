using System.IO;
using ShotGridIntegration;

namespace DataWranglerServiceWorker
{
	public class FileCopyMetaData
	{
		public string SourceFilePath;
		public string DestinationRelativeFilePath;
		public string DestinationFullFilePath;

		public ShotGridEntityLocalStorage StorageTarget;

		public FileCopyMetaData(string a_sourceFilePath, string a_destinationRelativeFilePath, ShotGridEntityLocalStorage a_storageTarget)
		{
			SourceFilePath = a_sourceFilePath;
			DestinationRelativeFilePath = a_destinationRelativeFilePath;
			StorageTarget = a_storageTarget;
			DestinationFullFilePath = Path.Combine(a_storageTarget.Attributes.WindowsPath, a_destinationRelativeFilePath);
		}
	}
}
