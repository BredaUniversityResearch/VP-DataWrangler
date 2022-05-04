
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
			return await FindAndParse<ShotGridEntityProject[]>("projects", filter, new[] { "name" });
		}

		public async Task<ShotGridEntityShot[]?> GetShotsForProject(int a_projectId)
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			filter.FieldIs("project.Project.id", a_projectId);
			return await FindAndParse<ShotGridEntityShot[]>("shots", filter, new[] {"code", "description", "image" });
		}

		public async Task<ShotGridEntityShotVersion[]?> GetVersionsForShot(int a_shotId)
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			filter.FieldIs("entity.Shot.id", a_shotId);
			return await FindAndParse<ShotGridEntityShotVersion[]>("versions", filter, new[] { "code", "description", "image" });
		}

		private bool ParseResponse<TTargetType>(string a_responseString, out TTargetType? a_result, [NotNullWhen(false)] out ShotGridErrorResponse? a_error)
			where TTargetType: class
		{
			a_result = null;
			a_error = null;

			JObject parsedResult = JObject.Parse(a_responseString);
			if (parsedResult.ContainsKey("errors"))
			{
				a_error = parsedResult.ToObject<ShotGridErrorResponse>()!;
				return false;
			}
			else if (parsedResult.TryGetValue("data", out JToken? dataNode))
			{
				a_result = dataNode.ToObject<TTargetType>();
				return true;
			}

			throw new Exception($"Failure deserializing response {a_responseString}");
		}

		private async Task<TTargetType?> FindAndParse<TTargetType>(string a_entityType, ShotGridSimpleSearchFilter a_filters, string[] a_fields)
			where TTargetType : class
		{
			string result = await Find(a_entityType, a_filters, a_fields);
			if (ParseResponse(result, out TTargetType? resultEntities, out var errorResponse))
			{
				return resultEntities;
			}
			else
			{
				ReportError(errorResponse);
				return null;
			}
		}

		private void ReportError(ShotGridErrorResponse a_errorResponse)
		{
			throw new Exception("Api Exception: " + a_errorResponse);
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

		public async Task<ShotGridEntityShotVersion?> CreateNewShotVersion(int a_projectId, int a_parentShotId, string a_versionName)
		{
			ShotGridEntityShotVersion.ShotVersionAttributes version = new ShotGridEntityShotVersion.ShotVersionAttributes();
			version.VersionCode = a_versionName;

			JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			ShotGridEntityCreateBaseData baseData = new ShotGridEntityCreateBaseData(a_projectId, ShotGridEntity.TypeNames.Shot, a_parentShotId);
			JObject baseDataToken = JObject.FromObject(baseData, serializer);
			JObject encodedToken = JObject.FromObject(version, serializer);
			encodedToken.Merge(baseDataToken);

			string requestBody = encodedToken.ToString();

			string response = await Create("version", requestBody);

			if (ParseResponse(response, out ShotGridEntityShotVersion? createdEntity, out ShotGridErrorResponse? errorResponse))
			{
				return createdEntity;
			}
			else
			{
				ReportError(errorResponse);
				return null;
			}
		}

		public async Task<string> Create(string a_entityType, string a_entityDataAsString)
		{
			if (m_authentication == null)
			{
				throw new ShotGridAPIException("No authentication requested");
			}

			HttpRequestMessage request = new HttpRequestMessage
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri($"{BaseUrl}entity/{a_entityType}"),
				Headers = {
					{ HttpRequestHeader.Accept.ToString(), "application/json" },
					{ HttpRequestHeader.AcceptEncoding.ToString(), "gzip, deflate, br" },
					{ HttpRequestHeader.Authorization.ToString(), "Bearer " + m_authentication.AccessToken },
					{ HttpRequestHeader.ContentType.ToString(), "application/vnd+shotgun.api3_array+json; charset=utf-8" },
				},
				Content = new ByteArrayContent(Encoding.UTF8.GetBytes(a_entityDataAsString))
			};

			HttpResponseMessage response = await m_client.SendAsync(request);
			string result = await response.Content.ReadAsStringAsync();
			return result;
		}

		public void UpdateEntitySingleProperty(EShotGridEntity a_entity, int a_entityId, string a_propertyName, string a_value)
		{
			throw new NotImplementedException();
			//if (m_authentication == null)
			//{
			//	throw new ShotGridAPIException("No authentication requested");
			//}

			//HttpRequestMessage request = new HttpRequestMessage
			//{
			//	Method = HttpMethod.Post,
			//	RequestUri = new Uri($"{BaseUrl}entity/{a_entity}/{a_entityId}"),
			//	Headers = {
			//		{ HttpRequestHeader.Accept.ToString(), "application/json" },
			//		{ HttpRequestHeader.AcceptEncoding.ToString(), "gzip, deflate, br" },
			//		{ HttpRequestHeader.Authorization.ToString(), "Bearer " + m_authentication.AccessToken },
			//		{ HttpRequestHeader.ContentType.ToString(), "application/vnd+shotgun.api3_array+json; charset=utf-8" },
			//	},
			//	Content = new ByteArrayContent(Encoding.UTF8.GetBytes(a_entityDataAsString))
			//};

			//HttpResponseMessage response = await m_client.SendAsync(request);
			//string result = await response.Content.ReadAsStringAsync();
			//return result;
		}
	}
}