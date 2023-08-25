using System.Runtime.InteropServices.ComTypes;
using AutoNotify;
using DataApiCommon;

namespace DataWranglerCommon.IngestDataSources;

public partial class IngestShotVersionIdentifier
{
	private sealed class ShotVersionIdEqualityComparer : IEqualityComparer<IngestShotVersionIdentifier>
	{
		public bool Equals(IngestShotVersionIdentifier? x, IngestShotVersionIdentifier? y)
		{
			if (ReferenceEquals(x, y)) return true;
			if (ReferenceEquals(x, null)) return false;
			if (ReferenceEquals(y, null)) return false;
			if (x.GetType() != y.GetType()) return false;
			return x.m_shotVersionId.Equals(y.m_shotVersionId);
		}

		public int GetHashCode(IngestShotVersionIdentifier obj)
		{
			return obj.m_shotVersionId.GetHashCode();
		}
	}

	public static IEqualityComparer<IngestShotVersionIdentifier> ShotVersionIdComparer { get; } = new ShotVersionIdEqualityComparer();

	[AutoNotify]
	private Guid m_shotVersionId;

	[AutoNotify]
	private string m_shotVersionName;

	[AutoNotify]
	private string m_shotName = "Unknown";

	[AutoNotify]
	private string m_projectName = "Unknown";

	public IngestShotVersionIdentifier(DataEntityShotVersion a_data, DataEntityCache? a_cache)
	{
		m_shotVersionId = a_data.EntityId;
		m_shotVersionName = a_data.ShotVersionName;

		if (a_cache != null)
		{
			if (a_data.EntityRelationships.Parent != null)
			{
				DataEntityShot? shotData = a_cache.FindEntityById<DataEntityShot>(a_data.EntityRelationships.Parent.EntityId);
				if (shotData != null)
				{
					m_shotName = shotData.ShotName;
				}
			}

			if (a_data.EntityRelationships.Project != null)
			{
				DataEntityProject? project = a_cache.FindEntityById<DataEntityProject>(a_data.EntityRelationships.Project.EntityId);
				if (project != null)
				{
					m_projectName = project.Name;
				}
			}
		}
	}
};