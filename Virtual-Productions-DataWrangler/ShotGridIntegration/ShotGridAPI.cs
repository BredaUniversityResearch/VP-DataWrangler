
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ShotGridIntegration
{
	public class ShotGridAPI
	{
		private const string BaseUrl = "https://buas.shotgunstudio.com/api/v1/";
		private const string ApiEndpointLogin = "auth/access_token";

		private HttpClient m_client = new HttpClient();
		private ShotGridAuthentication? m_authentication = null;

		//Password is the ShotGrid passphrase, NOT the Autodesk ID password.
		public async Task<ShotGridLoginResponse> TryLogin(string a_username, string a_password)
		{
			Dictionary<string, string> loginHeaders = new Dictionary<string, string>
			{
				{ "username", a_username },
				{ "password", a_password },
				{ "grant_type", "password" }
			};
			return await TryLogin(loginHeaders);
		}

		public async Task<ShotGridLoginResponse> TryLogin(string a_refreshToken)
		{
			Dictionary<string, string> loginHeaders = new Dictionary<string, string>
			{
				{ "refresh_token", a_refreshToken },
				{ "grant_type", "refresh_token" }
			};
			return await TryLogin(loginHeaders);
		}

		private async Task<ShotGridLoginResponse> TryLogin(Dictionary<string, string> a_loginHeaders)
		{
			HttpRequestMessage request = new HttpRequestMessage
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri(BaseUrl + ApiEndpointLogin),
				Headers = {
					{ HttpRequestHeader.Accept.ToString(), "application/json" },
					{ HttpRequestHeader.AcceptEncoding.ToString(), "gzip, deflate, br" }
				},
				Content = new FormUrlEncodedContent(a_loginHeaders)
			};

			HttpResponseMessage responseAuth = await m_client.SendAsync(request);
			string responseString = await responseAuth.Content.ReadAsStringAsync();
			if (!responseAuth.IsSuccessStatusCode)
			{
				ShotGridErrorResponse? errorResponse = JsonConvert.DeserializeObject<ShotGridErrorResponse>(responseString);

				return new ShotGridLoginResponse(false, errorResponse);
			}

			APIAuthResponse? deserializedResponse = JsonConvert.DeserializeObject<APIAuthResponse>(responseString);
			if (deserializedResponse != null)
			{
				m_authentication = new ShotGridAuthentication(deserializedResponse);
				return new ShotGridLoginResponse(true, null);
			}
			else
			{
				return new ShotGridLoginResponse(false, null);
			}
		}

		public async Task<ShotGridEntityProject[]?> GetProjects()
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			filter.FieldIs("sg_status", "Active");
			string result = await Find("projects", filter, new[] {"name"});

			JObject parsedResult = JObject.Parse(result);
			ShotGridEntityProject[]? resultProjects = null;
			if (parsedResult.TryGetValue("data", out JToken? dataNode))
			{
				resultProjects = dataNode.ToObject<ShotGridEntityProject[]>();
			}

			return resultProjects;
		}

		public async Task<string> Find(string a_entityType, ShotGridSimpleSearchFilter a_filters, string[] a_fields)
		{
			if (m_authentication == null)
			{
				throw new ShotGridAPIException("No authentication requested");
			}

			ShotGridSearchQuery query = new ShotGridSearchQuery(a_filters, a_fields);
			string queryAsString = JsonConvert.SerializeObject(query);

			HttpRequestMessage request = new HttpRequestMessage
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri($"{BaseUrl}entity/{a_entityType}/_search"),
				Headers = {
					{ HttpRequestHeader.Accept.ToString(), "application/json" },
					{ HttpRequestHeader.AcceptEncoding.ToString(), "gzip, deflate, br" },
					{ HttpRequestHeader.Authorization.ToString(), "Bearer " + m_authentication.AccessToken },
					{ HttpRequestHeader.ContentType.ToString(), "application/vnd+shotgun.api3_array+json; charset=utf-8" },
				},
				Content = new ByteArrayContent(Encoding.UTF8.GetBytes(queryAsString))
			};

			HttpResponseMessage response = await m_client.SendAsync(request);
			string result = await response.Content.ReadAsStringAsync();
			return result;
		}

		public ShotGridAuthentication GetCurrentCredentials()
		{
			if (m_authentication == null)
			{
				throw new Exception("User not logged in");
			}

			return m_authentication;
		}
	}
}