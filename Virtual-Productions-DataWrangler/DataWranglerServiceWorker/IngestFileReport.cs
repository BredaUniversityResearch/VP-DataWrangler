using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using DataApiCommon;
using DataWranglerCommon.IngestDataSources;

namespace DataWranglerServiceWorker
{
	public class IngestFileReport
	{
		private List<IngestFileReportEntry> SynchronizedEntries = new List<IngestFileReportEntry>();
        public ObservableCollection<IngestFileReportEntry> Entries { get; } = new ObservableCollection<IngestFileReportEntry>();

        public void AddFileResolutionDetails(IngestFileResolutionDetails a_sourceFile)
        {
	        IngestFileReportEntry entry = GetEntryForFilePath(new Uri(a_sourceFile.FilePath));
	        entry.IngestReport = a_sourceFile.Rejections;
        }

        private IngestFileReportEntry GetEntryForFilePath(Uri a_fileSourcePath)
        {
	        lock (SynchronizedEntries)
	        {
		        foreach (IngestFileReportEntry entry in Entries)
		        {
			        if (entry.SourceFile == a_fileSourcePath)
			        {
				        return entry;
			        }
		        }

		        IngestFileReportEntry newEntry = new IngestFileReportEntry(a_fileSourcePath);
				SynchronizedEntries.Add(newEntry);
		        Application.Current.Dispatcher.InvokeAsync(() => Entries.Add(newEntry));
		        return newEntry;
	        }
        }

        public void AddCopiedFile(DataEntityShotVersion a_shotVersion, FileCopyMetaData a_copyMetaData, DataImportWorker.ECopyResult a_copyResult)
        {
	        IngestFileReportEntry entry = GetEntryForFilePath(a_copyMetaData.SourceFilePath);

	        entry.Status = a_copyResult.ToString();
	        entry.DestinationFile = a_copyMetaData.DestinationFullFilePath;
        }
    }
}
