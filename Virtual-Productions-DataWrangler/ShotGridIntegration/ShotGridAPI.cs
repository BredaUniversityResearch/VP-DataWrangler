
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

		public async Task<ShotGridAPIResponse<ShotGridEntityProject[]>> GetActiveProjects()
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			filter.FieldIs("sg_status", "Active");
			return await FindAndParse<ShotGridEntityProject[]>("projects", filter, new[] { "name" });
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityShot[]>> GetShotsForProject(int a_projectId)
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			filter.FieldIs("project.Project.id", a_projectId);
			return await FindAndParse<ShotGridEntityShot[]>("shots", filter, new[] {"code", "description", "image" });
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityShotVersion[]>> GetVersionsForShot(int a_shotId)
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			filter.FieldIs("entity.Shot.id", a_shotId);
			return await FindAndParse<ShotGridEntityShotVersion[]>("versions", filter, new[] { "code", "description", "image", "sg_datawrangler_meta" });
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityFilePublish[]>> GetPublishesForShotVersion(int a_shotVersionId)
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			filter.FieldIs("version.Version.id", a_shotVersionId);
			return await FindAndParse<ShotGridEntityFilePublish[]>(ShotGridEntity.TypeNames.PublishedFile, filter, 
				new JsonAttributeFieldEnumerator<ShotGridEntityFilePublish.FilePublishAttributes>().Get());
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityRelation[]>> GetPublishFileTypes(int a_projectId)
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			//filter.FieldIs("project.Project.id", a_projectId);
			return await FindAndParse<ShotGridEntityRelation[]>(ShotGridEntity.TypeNames.PublishedFileType, filter, 
				new JsonAttributeFieldEnumerator<ShotGridEntityRelation.RelationAttributes>().Get());
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityLocalStorage[]>> GetLocalStorages()
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			return await FindAndParse<ShotGridEntityLocalStorage[]>(ShotGridEntity.TypeNames.LocalStorage, filter,
				new JsonAttributeFieldEnumerator<ShotGridEntityLocalStorage.LocalStorageAttributes>().Get());
		}

		private ShotGridAPIResponse<TTargetType> ParseResponse<TTargetType>(HttpStatusCode a_statusCode, string a_responseString)
			where TTargetType: class
		{
			ShotGridErrorResponse? error = null;
			TTargetType? data = null;

			JObject parsedResult = JObject.Parse(a_responseString);

			if (((int)a_statusCode >= 200) && ((int)a_statusCode <= 299))
			{
				JToken? dataNode = parsedResult["data"];
				if (dataNode != null)
				{
					data = dataNode.ToObject<TTargetType>();
				}
				else
				{
					error = new ShotGridErrorResponse
					{
						Errors = new ShotGridErrorResponse.RequestError[]
						{
							new() {Detail = "Invalid response returned from ShotGrid, missing 'data' node in root Json object"}
						}
					};
				}

			}
			else
			{
				error = parsedResult.ToObject<ShotGridErrorResponse>();
			}

			if (error != null)
			{
				ReportError(error);
			}

			return new ShotGridAPIResponse<TTargetType>(data, error);
		}

		private void ReportError(ShotGridErrorResponse a_errorResponse)
		{
			//Todo: Add logging here.
		}

		private async Task<ShotGridAPIResponse<TTargetType>> FindAndParse<TTargetType>(string a_entityType, ShotGridSimpleSearchFilter a_filters, string[] a_fields)
			where TTargetType : class
		{
			HttpResponseMessage result = await Find(a_entityType, a_filters, a_fields);
			string resultBody = await result.Content.ReadAsStringAsync();
			return ParseResponse<TTargetType>(result.StatusCode, resultBody);
		}

		private async Task<HttpResponseMessage> Find(string a_entityType, ShotGridSimpleSearchFilter a_filters, string[] a_fields, bool a_allowReAuthenticate = true)
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

			if (response.StatusCode == HttpStatusCode.Unauthorized && a_allowReAuthenticate)
			{
				ShotGridLoginResponse loginResponse = await TryLogin(m_authentication.RefreshToken);
				if (loginResponse.Success)
				{
					response = await Find(a_entityType, a_filters, a_fields, false);
				}
				else
				{
					throw new ShotGridAPIException(loginResponse.ErrorResponse.ToString());
				}
			}

			return response;
		}

		public ShotGridAuthentication GetCurrentCredentials()
		{
			if (m_authentication == null)
			{
				throw new Exception("User not logged in");
			}

			return m_authentication;
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityShotVersion>> CreateNewShotVersion(int a_projectId, int a_parentShotId, ShotGridEntityShotVersion.ShotVersionAttributes a_versionAttributes)
		{
			JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			ShotGridEntityCreateBaseData baseData = new ShotGridEntityCreateBaseData(a_projectId, ShotGridEntity.TypeNames.Shot, a_parentShotId);
			JObject baseDataToken = JObject.FromObject(baseData, serializer);
			JObject encodedToken = JObject.FromObject(a_versionAttributes, serializer);
			encodedToken.Merge(baseDataToken);

			string requestBody = encodedToken.ToString();

			HttpResponseMessage response = await Create("version", requestBody);
			string responseBody = await response.Content.ReadAsStringAsync();

			return ParseResponse<ShotGridEntityShotVersion>(response.StatusCode, responseBody);
		}
		public async Task<ShotGridAPIResponse<ShotGridEntityFilePublish>> CreateFilePublish(int a_projectId, int a_shotId, int a_versionId, ShotGridEntityFilePublish.FilePublishAttributes a_publishAttributes)
		{
			JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			});


			ShotGridEntityCreateBaseData baseData = new ShotGridEntityCreateBaseData(a_projectId, ShotGridEntity.TypeNames.Shot, a_shotId);
			JObject baseDataToken = JObject.FromObject(baseData, serializer);
			JObject encodedToken = JObject.FromObject(a_publishAttributes, serializer);
			encodedToken.Merge(baseDataToken);

			string requestBody = encodedToken.ToString();

			HttpResponseMessage response = await Create(ShotGridEntity.TypeNames.PublishedFile, requestBody);
			string responseBody = await response.Content.ReadAsStringAsync();

			return ParseResponse<ShotGridEntityFilePublish>(response.StatusCode, responseBody);
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityAttachment>> CreateFileAttachment(int a_projectId, int a_publishId, ShotGridEntityAttachment a_attachmentAttributes)
		{
			JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			ShotGridEntityCreateBaseData baseData = new ShotGridEntityCreateBaseData(a_projectId, ShotGridEntity.TypeNames.PublishedFile, a_publishId);
			JObject baseDataToken = JObject.FromObject(baseData, serializer);
			JObject encodedToken = JObject.FromObject(a_attachmentAttributes, serializer);
			encodedToken.Merge(baseDataToken);

			string requestBody = encodedToken.ToString();

			HttpResponseMessage response = await Create(ShotGridEntity.TypeNames.Attachment, requestBody);
			string responseBody = await response.Content.ReadAsStringAsync();

			return ParseResponse<ShotGridEntityAttachment>(response.StatusCode, responseBody);
		}

		private async Task<HttpResponseMessage> Create(string a_entityType, string a_entityDataAsString)
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
			return response;
		}

		public async Task<ShotGridAPIResponse<TEntityType>> UpdateEntityProperties<TEntityType>(int a_entityId, Dictionary<string, object> a_propertiesToSet)
			where TEntityType: ShotGridEntity
		{
			string entityType = ShotGridEntity.GetEntityName<TEntityType>();

			if (m_authentication == null)
			{
				throw new ShotGridAPIException("No authentication requested");
			}

			string fullRequestAsString = JsonConvert.SerializeObject(a_propertiesToSet);

			HttpRequestMessage request = new HttpRequestMessage
			{
				Method = HttpMethod.Put,
				RequestUri = new Uri($"{BaseUrl}entity/{entityType}/{a_entityId}"),
				Headers = {
					{ HttpRequestHeader.Accept.ToString(), "application/json" },
					{ HttpRequestHeader.AcceptEncoding.ToString(), "gzip, deflate, br" },
					{ HttpRequestHeader.Authorization.ToString(), "Bearer " + m_authentication.AccessToken },
					{ HttpRequestHeader.ContentType.ToString(), "application/vnd+shotgun.api3_array+json; charset=utf-8" },
				},
				Content = new ByteArrayContent(Encoding.UTF8.GetBytes(fullRequestAsString))
			};

			HttpResponseMessage response = await m_client.SendAsync(request);
			string responseBody = await response.Content.ReadAsStringAsync();

			return ParseResponse<TEntityType>(response.StatusCode, responseBody);
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityFieldSchema[]>> GetEntityFieldSchema(string a_entityType, int a_projectId)
		{
			if (m_authentication == null)
			{
				throw new ShotGridAPIException("No authentication requested");
			}

			HttpRequestMessage request = new HttpRequestMessage
			{
				Method = HttpMethod.Get,
				RequestUri = new Uri($"{BaseUrl}schema/{a_entityType}/fields?project_id={a_projectId}"),
				Headers = {
					{ HttpRequestHeader.Accept.ToString(), "application/json" },
					{ HttpRequestHeader.AcceptEncoding.ToString(), "gzip, deflate, br" },
					{ HttpRequestHeader.Authorization.ToString(), "Bearer " + m_authentication.AccessToken },
				}
			};

			HttpResponseMessage response = await m_client.SendAsync(request);
			string responseBody = await response.Content.ReadAsStringAsync();

			return ParseResponse<ShotGridEntityFieldSchema[]>(response.StatusCode, responseBody);
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityRelation?>> FindRelationByCode(string a_relationType, string a_code)
		{
			ShotGridAPIResponse<ShotGridEntityRelation[]> relations = await FindAndParse<ShotGridEntityRelation[]>(a_relationType, new ShotGridSimpleSearchFilter(), 
				new JsonAttributeFieldEnumerator<ShotGridEntityRelation.RelationAttributes>().Get());
			if (relations.IsError)
			{
				return new ShotGridAPIResponse<ShotGridEntityRelation?>(null, relations.ErrorInfo);
			}
			else
			{
				foreach (ShotGridEntityRelation relation in relations.ResultData)
				{
					if (string.Compare(relation.Attributes.Code, a_code, StringComparison.InvariantCultureIgnoreCase) == 0)
					{
						return new ShotGridAPIResponse<ShotGridEntityRelation?>(relation, null);
					}
				}

				return new ShotGridAPIResponse<ShotGridEntityRelation?>(null, new ShotGridErrorResponse
				{
					Errors = new[]
					{
						new ShotGridErrorResponse.RequestError {Title = $"Unable to find relation by code {a_code}"}
					}
				});
			}
		}
	}

	public class ShotGridEntityFieldSchema
	{
	};
}