namespace DataWranglerServiceWorker;

public class FileCopyProgress
{
	public readonly long TotalFileSizeBytes;
	public readonly long TotalBytesCopied;
	public readonly float PercentageCopied;
	public readonly long CurrentCopySpeedBytesPerSecond;

	public FileCopyProgress(long a_totalFileSizeBytes, long a_totalBytesCopied, float a_percentageCopied, long a_currentCopySpeedBytesPerSecond)
	{
		TotalFileSizeBytes = a_totalFileSizeBytes;
		TotalBytesCopied = a_totalBytesCopied;
		PercentageCopied = a_percentageCopied;
		CurrentCopySpeedBytesPerSecond = a_currentCopySpeedBytesPerSecond;
	}
};