using DataWranglerCommon.CameraHandling;
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
    public delegate void RecordingStartedDelegate(ActiveCameraInfo a_sourceCamera, IngestDataShotVersionMeta a_shotMetaData);
    public event RecordingStartedDelegate OnRecordingStarted = delegate { };

	public delegate void RecordingFinishedDelegate(ActiveCameraInfo a_sourceCamera, IngestDataShotVersionMeta a_shotMetaData);
	public event RecordingFinishedDelegate OnRecordingFinished = delegate { };

	public delegate void ShotCreatedDelegate();
	public event ShotCreatedDelegate OnShotCreated = delegate { };

    public void NotifyRecordingStarted(ActiveCameraInfo a_sourceCamera, IngestDataShotVersionMeta a_shotMetaData)
    {
        OnRecordingStarted(a_sourceCamera, a_shotMetaData);
    }

    public void NotifyRecordingFinished(ActiveCameraInfo a_sourceCamera, IngestDataShotVersionMeta a_shotMetaData)
    {
	    OnRecordingFinished(a_sourceCamera, a_shotMetaData);
    }

    public void NotifyShotCreated(int a_shotId)
    {
	    OnShotCreated();
    }
}