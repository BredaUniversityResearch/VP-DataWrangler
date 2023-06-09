using System.Text;

namespace DataApiCommon;

public class DataApiErrorDetails
{
	public class RequestError
	{
		public string Description = ""; //A short, human-readable summary of the problem.
	};

	public RequestError[] Errors = Array.Empty<RequestError>();

	public override string ToString()
	{
		StringBuilder sb = new StringBuilder(256);
		sb.Append("ShotGridErrorResponse: \n");
		foreach (RequestError error in Errors)
		{
			sb.Append($"{error.Description}");
		}

		return sb.ToString();
	}
};