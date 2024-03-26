namespace CameraControlOverEthernet;

public class ShotRecordingStatePacket : INetworkAPIPacket
{
	public string ProjectName;
	public string ShotName;
	public string ShotVersionName;
	public string NextShotVersionName;

	public ShotRecordingStatePacket(string a_projectName, string a_shotName, string a_shotVersionName, string a_nextShotVersionName)
	{
		ProjectName = a_projectName;
		ShotName = a_shotName;
		ShotVersionName = a_shotVersionName;
		NextShotVersionName = a_nextShotVersionName;
	}
}