using Newtonsoft.Json;

namespace ShotGridIntegration;

public class ShotGridFileLink
{
	public ShotGridFileLink()
	{
	}

	public ShotGridFileLink(Uri a_destinationPath)
	{
		FileName = Path.GetFileName(a_destinationPath.LocalPath);
		LinkType = "local";
		LocalPath = a_destinationPath.LocalPath;
		LocalPathWindows = a_destinationPath.LocalPath;
		Url = a_destinationPath.ToString();
	}

	[JsonProperty("link_type")]
	public string LinkType = "local";
	[JsonProperty("local_storage")]
	public ShotGridEntityReference? LocalStorageTarget;
	[JsonProperty("local_path")]
	public string? LocalPath;
	[JsonProperty("local_path_linux")]
	public string? LocalPathLinux;
	[JsonProperty("local_path_mac")]
	public string? LocalPathMac;
	[JsonProperty("local_path_windows")]
	public string? LocalPathWindows;
	[JsonProperty("name")]
	public string FileName = "";
	[JsonProperty("url")]
	public string Url = "file:///";
};