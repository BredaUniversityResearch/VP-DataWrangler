using DataApiCommon;

namespace DataWranglerCommon.IngestDataSources;

public class IngestFileResolutionDetails
{
	public readonly string FilePath;
	public Dictionary<IngestShotVersionIdentifier, string> Rejections = new Dictionary<IngestShotVersionIdentifier, string>(IngestShotVersionIdentifier.ShotVersionIdComparer);

	public IngestFileResolutionDetails(string a_filePath)
	{
		FilePath = a_filePath;
	}

	public void AddRejection(IngestShotVersionIdentifier a_shotVersion, string a_rejectionReason)
	{
		Rejections.Add(a_shotVersion, a_rejectionReason);
	}
}