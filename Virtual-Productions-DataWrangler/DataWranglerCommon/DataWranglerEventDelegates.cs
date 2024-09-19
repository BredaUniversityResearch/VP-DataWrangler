using DataApiCommon;
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
	//Invoked after ra recording started event has been received, before the metadata is cloned from template data. The data contained is the file sources template.
	public event RecordingStartedDelegate OnRecordingStartedBeforeShotDataCreated = delegate { };

	public delegate void RecordingFinishedDelegate(ActiveCameraInfo a_sourceCamera, IngestDataShotVersionMeta a_shotMetaData);
	public event RecordingFinishedDelegate OnRecordingFinished = delegate { };

	public delegate void ShotCreatedDelegate();
	public event ShotCreatedDelegate OnShotCreated = delegate { };

	public void NotifyRecordingStartedBeforeShotDataCreated(ActiveCameraInfo a_sourceCamera, IngestDataShotVersionMeta a_templateMetaData)
	{
		OnRecordingStartedBeforeShotDataCreated(a_sourceCamera, a_templateMetaData);
	}

	public void NotifyRecordingStarted(ActiveCameraInfo a_sourceCamera, IngestDataShotVersionMeta a_shotMetaData)
    {
        OnRecordingStarted(a_sourceCamera, a_shotMetaData);
    }

    public void NotifyRecordingFinished(ActiveCameraInfo a_sourceCamera, IngestDataShotVersionMeta a_shotMetaData)
    {
	    OnRecordingFinished(a_sourceCamera, a_shotMetaData);
    }

    public void NotifyShotCreated(Guid a_shotId)
    {
	    OnShotCreated();
    }
}