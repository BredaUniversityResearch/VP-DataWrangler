using System;
using System.Collections.Generic;
using AutoNotify;
using DataWranglerCommon.IngestDataSources;

namespace DataWranglerServiceWorker;

public partial class IngestFileReportEntry
{
	public IngestFileReportEntry(Uri a_sourceFile)
	{
		m_sourceFile = a_sourceFile;
	}

	[AutoNotify]
	private string m_status = "Not Imported";
	[AutoNotify]
	private Uri m_sourceFile;
	[AutoNotify]
	private Uri? m_destinationFile = null;

	[AutoNotify]
	private Dictionary<IngestShotVersionIdentifier, string>? m_ingestReport = null;
};