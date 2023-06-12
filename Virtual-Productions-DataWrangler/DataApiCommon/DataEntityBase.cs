using AutoNotify;

namespace DataApiCommon
{
	public partial class DataEntityBase
	{
		public Guid EntityId = Guid.Empty;

		[AutoNotify]
		private DataEntityRelationships m_entityRelationships = new DataEntityRelationships();

		public readonly DataEntityChangeTracker ChangeTracker;

		public DataEntityBase()
		{
			ChangeTracker = new DataEntityChangeTracker(this);
		}
	}

	public partial class DataEntityRelationships
	{
		public DataEntityReference? Project = null;
		public DataEntityReference? Parent = null;
	};

}