using System;
using System.IO;
using DataApiCommon;
using ShotGridIntegration;

namespace DataWranglerServiceWorker
{
	public class FileCopyMetaData
	{
		public Uri SourceFilePath;
		public string DestinationRelativeFilePath;
		public Uri DestinationFullPath;

		public DataEntityLocalStorage StorageTarget;
		public DataEntityPublishedFileType FileTag;

		public FileCopyMetaData(string a_sourceFilePath, string a_destinationRelativeFilePath, DataEntityLocalStorage a_storageTarget, DataEntityPublishedFileType a_fileTag)
		{
			SourceFilePath = new Uri(a_sourceFilePath, UriKind.Absolute);
			DestinationRelativeFilePath = a_destinationRelativeFilePath;
			StorageTarget = a_storageTarget;
			FileTag = a_fileTag;

			if (a_storageTarget.StorageRoot == null)
			{
				throw new Exception("Storage root was null when creating FileCopyMeta");
			}

			//string storageTargetPath = a_storageTarget.StorageRoot.LocalPath;
			//if (!storageTargetPath.EndsWith('/'))
			//{
			//	storageTargetPath += '/';
			//}

			DestinationFullPath = new Uri(a_storageTarget.StorageRoot, a_destinationRelativeFilePath);
		}
	}
}
