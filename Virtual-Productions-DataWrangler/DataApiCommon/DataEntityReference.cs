﻿using AutoNotify;

namespace DataApiCommon
{
	public partial class DataEntityReference
	{
		[AutoNotify]
		private Type? m_entityType = null;
		[AutoNotify]
		private int m_entityId = 0;
		[AutoNotify]
		private string? m_entityName = null;

		public DataEntityReference()
		{
		}

		public DataEntityReference(DataEntityBase a_entity)
		{
			m_entityType = a_entity.GetType();
			m_entityId = a_entity.EntityId;
			m_entityName = "???";
		}

		public DataEntityReference(Type a_entityType, int a_entityId)
		{
			m_entityType = a_entityType;
			m_entityId = a_entityId;
		}
	}
}
