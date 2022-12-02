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
		public ShotGridEntityRelation FileTag;

		public FileCopyMetaData(string a_sourceFilePath, string a_destinationRelativeFilePath, ShotGridEntityLocalStorage a_storageTarget, ShotGridEntityRelation a_fileTag)
		{
			SourceFilePath = new Uri(a_sourceFilePath, UriKind.Absolute);
			DestinationRelativeFilePath = a_destinationRelativeFilePath;
			StorageTarget = a_storageTarget;
			FileTag = a_fileTag;

			string storageTargetPath = a_storageTarget.Attributes.WindowsPath;
			if (!storageTargetPath.EndsWith('/'))
			{
				storageTargetPath += '/';
			}

			DestinationFullFilePath = new Uri(new Uri(storageTargetPath), a_destinationRelativeFilePath);
		}
	}
}
