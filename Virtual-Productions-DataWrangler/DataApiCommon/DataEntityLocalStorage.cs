using AutoNotify;

namespace DataApiCommon
{
	public partial class DataEntityLocalStorage: DataEntityBase
	{
		[AutoNotify]
		private string m_localStorageName = "";

		[AutoNotify]
		private Uri? m_storageRoot = null;

		[AutoNotify]
		private Uri? m_browsableLocalStorageRoot = null;
	}
}
