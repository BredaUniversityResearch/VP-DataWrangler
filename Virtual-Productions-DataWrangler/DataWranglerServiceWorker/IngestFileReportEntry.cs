using System;
using System.Collections.Generic;
using AutoNotify;
using DataWranglerCommon.IngestDataSources;

namespace DataWranglerServiceWorker;

public partial class IngestFileReportEntry
{
	public enum EStatusImageType
	{
		Success,
		Warning,
		Error,
		Pending
	};

	[AutoNotify]
	private EStatusImageType m_statusImageType = EStatusImageType.Warning;
	[AutoNotify]
	private string m_status = "Ignored File Type";
	[AutoNotify]
	private Uri m_sourceFile;
	[AutoNotify]
	private Uri? m_destinationFile = null;

	[AutoNotify]
	private Dictionary<IngestShotVersionIdentifier, string>? m_ingestReport = null;

	public IngestFileReportEntry(Uri a_sourceFile)
	{
		m_sourceFile = a_sourceFile;
	}
};