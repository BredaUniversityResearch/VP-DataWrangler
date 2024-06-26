﻿using System.Reflection;
using DataApiCommon;
using Newtonsoft.Json;

namespace ShotGridIntegration;

internal abstract class ShotGridEntity
{

	[JsonProperty("id")]
	public int Id;

	[JsonProperty("type")]
	public ShotGridEntityTypeInfo ShotGridType;

	[JsonProperty("links")]
	public ShotGridEntityLinks Links = new ShotGridEntityLinks();

	[JsonProperty("relationships")]
	public ShotGridEntityRelationships EntityRelationships = new ShotGridEntityRelationships();

	protected ShotGridEntity()
	{
		ShotGridType = ShotGridEntityTypeInfo.FromType(GetType());
	}

	protected ShotGridEntity(DataEntityBase a_entity)
		: this()
	{
		Id = ShotGridIdUtility.ToShotGridId(a_entity.EntityId);
		if (a_entity.EntityRelationships.Parent != null)
		{
			EntityRelationships.Parent = new ShotGridEntityReference(a_entity.EntityRelationships.Parent);
		}

		if (a_entity.EntityRelationships.Project != null)
		{
			EntityRelationships.Project = new ShotGridEntityReference(a_entity.EntityRelationships.Project);
		}
	}

	public void CopyToDataEntity(DataEntityBase a_entity)
	{
		a_entity.EntityId = ShotGridIdUtility.ToDataEntityId(Id);
		a_entity.EntityRelationships.Parent = EntityRelationships.Parent?.ToDataEntity();
		a_entity.EntityRelationships.Project = EntityRelationships.Project?.ToDataEntity();
	}

	protected abstract DataEntityBase ToDataEntityInternal();

	public DataEntityBase ToDataEntity()
	{
		DataEntityBase result = ToDataEntityInternal();
		CopyToDataEntity(result);
		result.ChangeTracker.ClearChangedState();
		return result;
	}

}

internal class ShotGridEntityRelationships
{
	[JsonProperty("project"), JsonConverter(typeof(JsonConverterShotGridEntityReferenceRelationships))]
	public ShotGridEntityReference? Project;
	[JsonProperty("entity"), JsonConverter(typeof(JsonConverterShotGridEntityReferenceRelationships))]
	public ShotGridEntityReference? Parent;

	public ShotGridEntityRelationships()
	{
	}

	public ShotGridEntityRelationships(DataEntityRelationships a_relationships)
	{
		if (a_relationships.Project != null)
		{
			Project = new ShotGridEntityReference(a_relationships.Project);
		}

		if (a_relationships.Parent != null)
		{
			Parent = new ShotGridEntityReference(a_relationships.Parent);
		}
	}
}