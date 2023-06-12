using DataApiCommon;
using Newtonsoft.Json;

namespace ShotGridIntegration;

internal class ShotGridFileLink
{
	public ShotGridFileLink()
	{
	}

	public ShotGridFileLink(DataEntityFileLink a_fileLink)
	{
		if (a_fileLink.UriPath != null)
		{
			FromUri(a_fileLink.UriPath);
		}
	}

	public ShotGridFileLink(Uri a_destinationPath)
	{
		FromUri(a_destinationPath);
	}

	private void FromUri(Uri a_uri)
	{
		FileName = Path.GetFileName(a_uri.LocalPath);
		LinkType = "local";
		LocalPath = a_uri.LocalPath;
		LocalPathWindows = a_uri.LocalPath;
		Url = a_uri.ToString();
	}

	[JsonProperty("link_type")]
	public string LinkType = "local";
	[JsonProperty("local_storage")]
	public ShotGridEntityReference? LocalStorageTarget = null;
	[JsonProperty("local_path")]
	public string? LocalPath = null;
	[JsonProperty("local_path_linux")]
	public string? LocalPathLinux = null;
	[JsonProperty("local_path_mac")]
	public string? LocalPathMac = null;
	[JsonProperty("local_path_windows")]
	public string? LocalPathWindows = null;
	[JsonProperty("name")]
	public string FileName = "";
	[JsonProperty("url")]
	public string Url = "file:///";

	public DataEntityFileLink ToDataEntity()
	{
		DataEntityFileLink link = new DataEntityFileLink()
		{
			FileName = FileName,
			UriPath = new Uri(Url)
		};
		return link;
	}
};