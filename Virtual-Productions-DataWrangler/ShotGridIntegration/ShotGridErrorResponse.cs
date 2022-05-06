using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ShotGridIntegration
{
	public class ShotGridErrorResponse
	{
		public class RequestError
		{
			[JsonProperty("id")]
			public string Id = ""; //A unique identifier for this particular occurrence of the problem.
			[JsonProperty("status")]
			public int HttpStatus = 200;
			[JsonProperty("code")]
			public int ShotGridErrorCode = 0;
			[JsonProperty("title")]
			public string Title = ""; //A short, human-readable summary of the problem.
			[JsonProperty("detail")]
			public string Detail = ""; //A human-readable explanation specific to this occurrence of the problem.
			[JsonProperty("meta")]
			public string Meta = ""; //Non-standard meta-information about the error.

			//Missing: source - Object containing references to source - As of now don't know how to handle this.
		};

		[JsonProperty("errors")]
		public RequestError[] Errors = Array.Empty<RequestError>();

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder(256);
			sb.Append("ShotGridErrorResponse: \n");
			foreach (RequestError error in Errors)
			{
				sb.Append($"{error.Title}: {error.Detail}");
			}

			return sb.ToString();
		}
	}
}
