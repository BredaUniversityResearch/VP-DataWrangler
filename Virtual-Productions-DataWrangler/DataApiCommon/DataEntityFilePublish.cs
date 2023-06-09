using AutoNotify;
using Newtonsoft.Json;
using System;

namespace DataApiCommon
{
	public partial class DataEntityFilePublish: DataEntityBase
	{
		[AutoNotify]
		private string m_publishedFileName = "";
		[AutoNotify]
		private DataEntityFileLink? m_path;
		[AutoNotify]
		private string? m_relativePathToStorageRoot;
		[AutoNotify]
		private DataEntityReference? m_storageRoot;
		[AutoNotify]
		private DataEntityReference? m_publishedFileType;
		[AutoNotify]
		private DataEntityReference? m_shotVersion;
		[AutoNotify]
		private string m_description = "";
		//[JsonProperty("sg_status_list")]
		//public string Status = ShotGridStatusListEntry.WaitingToBegin;
	}
}
