using DataWranglerCommon;

namespace DataWranglerInterface;

public class DataWranglerEventDelegates
{

	//Invoked after a recording started event has been received, but before the meta data is submitted to the backend.
    public delegate void RecordingStartedDelegate(DataWranglerShotVersionMeta a_shotMetaData);
    public event RecordingStartedDelegate OnRecordingStarted = delegate { };

	public delegate void RecordingFinishedDelegate(DataWranglerShotVersionMeta a_shotMetaData);
	public event RecordingFinishedDelegate OnRecordingFinished = delegate { };

    public void NotifyRecordingStarted(DataWranglerShotVersionMeta a_shotMetaData)
    {
        OnRecordingStarted(a_shotMetaData);
    }

    public void NotifyRecordingFinished(DataWranglerShotVersionMeta a_shotMetaData)
    {
	    OnRecordingFinished(a_shotMetaData);
    }

    public void NotifyShotCreated(int a_shotId)
    {
		//Eh...
    }
}