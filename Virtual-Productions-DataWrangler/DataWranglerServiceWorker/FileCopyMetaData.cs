using System;
using System.IO;
using ShotGridIntegration;

namespace DataWranglerServiceWorker
{
	public class FileCopyMetaData
	{
		public Uri SourceFilePath;
		public string DestinationRelativeFilePath;
		public Uri DestinationFullFilePath;

		public ShotGridEntityLocalStorage StorageTarget;

		public FileCopyMetaData(string a_sourceFilePath, string a_destinationRelativeFilePath, ShotGridEntityLocalStorage a_storageTarget)
		{
			SourceFilePath = new Uri(a_sourceFilePath, UriKind.Absolute);
			DestinationRelativeFilePath = a_destinationRelativeFilePath;
			StorageTarget = a_storageTarget;
			DestinationFullFilePath = new Uri(new Uri(a_storageTarget.Attributes.WindowsPath), a_destinationRelativeFilePath);
		}
	}
}
