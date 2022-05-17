using System;

namespace DataWranglerServiceWorker;

public class ShotVersionIdentifier
{
	public readonly int ProjectId;
	public readonly int ShotId;
	public readonly int VersionId;

	public ShotVersionIdentifier(int a_projectId, int a_shotId, int a_versionId)
	{
		ProjectId = a_projectId;
		ShotId = a_shotId;
		VersionId = a_versionId;
	}

	private bool Equals(ShotVersionIdentifier a_other)
	{
		return ProjectId == a_other.ProjectId && ShotId == a_other.ShotId && VersionId == a_other.VersionId;
	}

	public override bool Equals(object? a_obj)
	{
		if (ReferenceEquals(null, a_obj)) return false;
		if (ReferenceEquals(this, a_obj)) return true;
		if (a_obj.GetType() != this.GetType()) return false;
		return Equals((ShotVersionIdentifier)a_obj);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(ProjectId, ShotId, VersionId);
	}
};