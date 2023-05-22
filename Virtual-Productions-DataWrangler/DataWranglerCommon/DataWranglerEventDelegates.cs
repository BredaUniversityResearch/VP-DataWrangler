using DataWranglerCommon.IngestDataSources;

namespace DataWranglerCommon;

public class DataWranglerEventDelegates
{
	public static DataWranglerEventDelegates Instance { get; }

	static DataWranglerEventDelegates()
	{
		Instance = new DataWranglerEventDelegates();
	}

	//Invoked after a recording started event has been received, but before the meta data is submitted to the backend.
    public delegate void RecordingStartedDelegate(IngestDataShotVersionMeta a_shotMetaData);
    public event RecordingStartedDelegate OnRecordingStarted = delegate { };

	public delegate void RecordingFinishedDelegate(IngestDataShotVersionMeta a_shotMetaData);
	public event RecordingFinishedDelegate OnRecordingFinished = delegate { };

    public void NotifyRecordingStarted(IngestDataShotVersionMeta a_shotMetaData)
    {
        OnRecordingStarted(a_shotMetaData);
    }

    public void NotifyRecordingFinished(IngestDataShotVersionMeta a_shotMetaData)
    {
	    OnRecordingFinished(a_shotMetaData);
    }

    public void NotifyShotCreated(int a_shotId)
    {
		//Eh...
    }
}