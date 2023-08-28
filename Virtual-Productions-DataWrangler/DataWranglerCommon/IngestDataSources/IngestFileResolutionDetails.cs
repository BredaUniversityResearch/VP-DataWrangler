using System.Diagnostics.CodeAnalysis;
using DataApiCommon;

namespace DataWranglerCommon.IngestDataSources;

public class IngestFileResolutionDetails
{
	public readonly string FilePath;
	public Dictionary<IngestShotVersionIdentifier, string> Rejections = new Dictionary<IngestShotVersionIdentifier, string>(IngestShotVersionIdentifier.ShotVersionIdComparer);

	public DataEntityShotVersion? TargetShotVersion = null;
	public string TargetFileTag = "";

	public IngestFileResolutionDetails(string a_filePath)
	{
		FilePath = a_filePath;
	}

	public void AddRejection(IngestShotVersionIdentifier a_shotVersion, string a_rejectionReason)
	{
		Rejections.Add(a_shotVersion, a_rejectionReason);
	}

	public void SetSuccessfulResolution(DataEntityShotVersion a_targetShotVersion, string a_fileTag)
	{
		TargetShotVersion = a_targetShotVersion;
		TargetFileTag = a_fileTag;
	}

	[MemberNotNullWhen(true, nameof(TargetShotVersion))]
	public bool HasSuccessfulResolution()
	{
		return TargetShotVersion != null;
	}
}