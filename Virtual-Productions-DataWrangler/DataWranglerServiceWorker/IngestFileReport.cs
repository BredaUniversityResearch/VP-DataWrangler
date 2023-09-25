﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using DataApiCommon;
using DataWranglerCommon.IngestDataSources;

namespace DataWranglerServiceWorker
{
	public class IngestFileReport
	{
		private readonly List<IngestFileReportEntry> SynchronizedEntries = new List<IngestFileReportEntry>();
        public ObservableCollection<IngestFileReportEntry> Entries { get; } = new ObservableCollection<IngestFileReportEntry>();

        public void AddFileResolutionDetails(IngestFileResolutionDetails a_sourceFile)
        {
	        IngestFileReportEntry entry = GetEntryForFilePath(new Uri(a_sourceFile.FilePath));
	        entry.IngestReport = a_sourceFile.Rejections;

	        if (a_sourceFile.HasSuccessfulResolution())
	        {
		        entry.Status = "Import Queued";
		        entry.StatusImageType = IngestFileReportEntry.EStatusImageType.Pending;
	        }
	        else if (a_sourceFile.Rejections.Count > 0)
	        {
		        entry.Status = "Not Imported";
		        entry.StatusImageType = IngestFileReportEntry.EStatusImageType.Error;
	        }
		}

        public IngestFileReportEntry? FindEntryForFilePath(Uri a_fileSourcePath)
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
	        }

			return null;
        }

        private IngestFileReportEntry GetEntryForFilePath(Uri a_fileSourcePath)
        {
	        lock (SynchronizedEntries)
	        {
		        IngestFileReportEntry? existingEntry = FindEntryForFilePath(a_fileSourcePath);
		        if (existingEntry != null)
		        {
			        return existingEntry;
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

	        switch (a_copyResult)
	        {
				default:
				case DataImportWorker.ECopyResult.InvalidDestinationPath:
				case DataImportWorker.ECopyResult.UnknownFailure:
					entry.StatusImageType = IngestFileReportEntry.EStatusImageType.Error;
					break;
				case DataImportWorker.ECopyResult.Success:
				case DataImportWorker.ECopyResult.FileAlreadyUpToDate:
					entry.StatusImageType = IngestFileReportEntry.EStatusImageType.Success;
					break;
	        }

	        entry.DestinationFile = a_copyMetaData.DestinationFullFilePath;
        }
    }
}