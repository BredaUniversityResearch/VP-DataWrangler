using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ShotGridIntegration
{
	public class ShotGridAPI
	{
		private const string BaseUrl = "https://buas.shotgunstudio.com/api/v1/";
		private const string ApiEndpointLogin = "auth/access_token";

		private static readonly TimeSpan AuthTokenRefreshTime = TimeSpan.FromMinutes(5);

		private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
			DateFormatHandling = DateFormatHandling.IsoDateFormat,
			Converters = new List<JsonConverter>() { 
				new JsonConverterShotGridEntityName()
			}
		};
		private static readonly JsonSerializer Serializer = JsonSerializer.Create(SerializerSettings);

		private HttpClient m_client = new HttpClient();
		private ShotGridAuthentication? m_authentication = null;
		private Task? m_tokenRefreshTask = null;
		private CancellationTokenSource m_cancellationTokenSource = new CancellationTokenSource();
		private Action? m_onRequestUserAuthentication = null;

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

		public async Task<ShotGridLoginResponse> TryLoginWithScriptKey(string a_clientId, string a_clientSecret)
		{
			Dictionary<string, string> loginHeaders = new Dictionary<string, string>
			{
				{ "client_id", a_clientId },
				{ "client_secret", a_clientSecret },
				{ "grant_type", "client_credentials" }
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
				ShotGridErrorResponse? errorResponse = JsonConvert.DeserializeObject<ShotGridErrorResponse>(responseString, SerializerSettings);

				return new ShotGridLoginResponse(false, errorResponse, null);
			}

			APIAuthResponse? deserializedResponse = JsonConvert.DeserializeObject<APIAuthResponse>(responseString, SerializerSettings);
			if (deserializedResponse != null)
			{
				m_authentication = new ShotGridAuthentication(deserializedResponse);
				return new ShotGridLoginResponse(true, null, deserializedResponse);
			}
			else
			{
				return new ShotGridLoginResponse(false, null, deserializedResponse);
			}
		}

		public async Task<ShotGridLoginResponse> TryRefreshToken()
		{
			if (m_authentication != null)
			{
				ShotGridLoginResponse response = await TryLogin(m_authentication.RefreshToken);
				if (response.Success)
				{
					m_authentication = new ShotGridAuthentication(response.AuthResponse);
				}

				return response;
			}
			else
			{
				throw new InvalidOperationException("Cannot use refresh token if none is available");
			}
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityProject[]>> GetActiveProjects()
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			filter.FieldIs("sg_status", "Active");
			return await FindAndParse<ShotGridEntityProject[]>(ShotGridEntityName.Project, filter, 
				new JsonAttributeFieldEnumerator<ShotGridEntityProject.ProjectAttributes>().Get(), null);
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityShot[]>> GetShotsForProject(int a_projectId)
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			filter.FieldIs("project.Project.id", a_projectId);
			return await FindAndParse<ShotGridEntityShot[]>(ShotGridEntityName.Shot, filter, 
				new JsonAttributeFieldEnumerator<ShotGridEntityShot.ShotAttributes>().Get(), null);
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityShotVersion[]>> GetVersionsForShot(int a_shotId, ShotGridSortSpecifier? a_sort = null)
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			filter.FieldIs("entity.Shot.id", a_shotId);
			return await FindAndParse<ShotGridEntityShotVersion[]>(ShotGridEntityName.ShotVersion, filter, 
				new JsonAttributeFieldEnumerator<ShotVersionAttributes>().Get(), a_sort);
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityFilePublish[]>> GetPublishesForShotVersion(int a_shotVersionId)
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			filter.FieldIs("version.Version.id", a_shotVersionId);
			return await FindAndParse<ShotGridEntityFilePublish[]>(ShotGridEntityName.PublishedFile, filter, 
				new JsonAttributeFieldEnumerator<ShotGridEntityFilePublish.FilePublishAttributes>().Get(), null);
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityRelation[]>> GetPublishFileTypes(int a_projectId)
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			//filter.FieldIs("project.Project.id", a_projectId);
			return await FindAndParse<ShotGridEntityRelation[]>(ShotGridEntityName.PublishedFileType, filter, 
				new JsonAttributeFieldEnumerator<ShotGridEntityRelation.RelationAttributes>().Get(), null);
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityLocalStorage[]>> GetLocalStorages()
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			return await FindAndParse<ShotGridEntityLocalStorage[]>(ShotGridEntityName.LocalStorage, filter,
				new JsonAttributeFieldEnumerator<ShotGridEntityLocalStorage.LocalStorageAttributes>().Get(), null);
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityRelation[]>> GetRelations(ShotGridEntityName a_relationType)
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			return await FindAndParse<ShotGridEntityRelation[]>(a_relationType, filter,
				new JsonAttributeFieldEnumerator<ShotGridEntityRelation.RelationAttributes>().Get(), null);
		}

		private bool ParseResponse(HttpStatusCode a_statusCode, string a_responseString, [NotNullWhen(true)] out JToken? a_successData, [NotNullWhen(false)] out ShotGridErrorResponse? a_error)
		{
			JObject parsedResult = JsonConvert.DeserializeObject<JObject>(a_responseString, SerializerSettings)!;
			a_error = null;
			a_successData = null;

			if (((int)a_statusCode >= 200) && ((int)a_statusCode <= 299))
			{
				JToken? dataNode = parsedResult["data"];
				if (dataNode != null)
				{
					a_successData = dataNode;
				}
				else
				{
					a_error = new ShotGridErrorResponse
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
				a_error = parsedResult.ToObject<ShotGridErrorResponse>();
			}

			if (a_error != null)
			{
				ReportError(a_error);
			}

			return a_successData != null;
		}

		private ShotGridAPIResponseGeneric ParseResponse(Type a_successType, HttpStatusCode a_statusCode, string a_responseString)
		{
			object? result = null;
			if (ParseResponse(a_statusCode, a_responseString, out JToken? dataData, out ShotGridErrorResponse? requestError))
			{
				result = dataData.ToObject(a_successType, Serializer);
			}
			return new ShotGridAPIResponseGeneric(result, requestError);
		}

		private ShotGridAPIResponse<TTargetType> ParseResponse<TTargetType>(HttpStatusCode a_statusCode, string a_responseString)
			where TTargetType: class
		{
			TTargetType? result = null;
			if (ParseResponse(a_statusCode, a_responseString, out JToken? dataData, out ShotGridErrorResponse? requestError))
			{
				result = dataData.ToObject<TTargetType>(Serializer);
			}
			return new ShotGridAPIResponse<TTargetType>(result, requestError);
		}

		private void ReportError(ShotGridErrorResponse a_errorResponse)
		{
			//Todo: Add logging here.
			throw new Exception(a_errorResponse.ToString());
		}

		private async Task<ShotGridAPIResponse<TTargetType>> FindAndParse<TTargetType>(ShotGridEntityName a_entityType, ShotGridSimpleSearchFilter a_filters, string[] a_fields, ShotGridSortSpecifier? a_sort)
			where TTargetType : class
		{
			HttpResponseMessage result = await Find(a_entityType, a_filters, a_fields, a_sort);
			string resultBody = await result.Content.ReadAsStringAsync();
			return ParseResponse<TTargetType>(result.StatusCode, resultBody);
		}

		private async Task<HttpResponseMessage> Find(ShotGridEntityName a_entityType, ShotGridSimpleSearchFilter a_filters, string[] a_fields, ShotGridSortSpecifier? a_sortSpecifier)
		{
			ShotGridSearchQuery query = new ShotGridSearchQuery(a_filters, a_fields, a_sortSpecifier);
			string queryAsString = JsonConvert.SerializeObject(query, SerializerSettings);
			return await SendApiRequest(new Uri($"{BaseUrl}entity/{a_entityType.SnakeCasePlural}/_search"), HttpMethod.Post, new ByteArrayContent(Encoding.UTF8.GetBytes(queryAsString)));
		}

		private async Task<HttpResponseMessage> SendApiRequest(Uri a_url, HttpMethod a_method, ByteArrayContent a_content, bool a_allowReAuthenticate = true)
		{
			if (m_authentication == null)
			{
				throw new ShotGridAPIException("No authentication requested");
			}

			HttpRequestMessage request = new HttpRequestMessage
			{
				Method = a_method,
				RequestUri = a_url,
				Headers = {
					{ HttpRequestHeader.Accept.ToString(), "application/json" },
					{ HttpRequestHeader.AcceptEncoding.ToString(), "gzip, deflate, br" },
					{ HttpRequestHeader.Authorization.ToString(), "Bearer " + m_authentication.AccessToken },
					{ HttpRequestHeader.ContentType.ToString(), "application/vnd+shotgun.api3_array+json; charset=utf-8" },
				},
				Content = a_content
			};

			HttpResponseMessage response = await m_client.SendAsync(request);

			if (response.StatusCode == HttpStatusCode.Unauthorized && a_allowReAuthenticate)
			{
				ShotGridLoginResponse loginResponse = await TryLogin(m_authentication.RefreshToken);
				if (loginResponse.Success)
				{
					response = await SendApiRequest(a_url, a_method, a_content, false);
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

		public async Task<ShotGridAPIResponse<ShotGridEntityShot>> CreateNewShot(int a_projectId, ShotGridEntityShot.ShotAttributes a_attributes)
		{
			return await CreateNewEntity<ShotGridEntityShot, ShotGridEntityShot.ShotAttributes>(a_projectId, a_attributes, null);
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityShotVersion>> CreateNewShotVersion(int a_projectId, int a_parentShotId, ShotVersionAttributes a_versionAttributes)
		{
			return await CreateNewEntity<ShotGridEntityShotVersion, ShotVersionAttributes>(a_projectId, a_versionAttributes, new ShotGridEntityReference(ShotGridEntityName.Shot, a_parentShotId));
			//ShotGridEntityCreateBaseData baseData = new ShotGridEntityCreateBaseData(a_projectId, ShotGridEntity.TypeNames.Shot, a_parentShotId);
			//JObject baseDataToken = JObject.FromObject(baseData, Serializer);
			//JObject encodedToken = JObject.FromObject(a_versionAttributes, Serializer);
			//encodedToken.Merge(baseDataToken);

			//string requestBody = encodedToken.ToString();

			//HttpResponseMessage response = await Create("version", requestBody);
			//string responseBody = await response.Content.ReadAsStringAsync();

			//return ParseResponse<ShotGridEntityShotVersion>(response.StatusCode, responseBody);
		}

		public async Task<ShotGridAPIResponse<TEntityType>> CreateNewEntity<TEntityType, TAttributesType>(int a_projectId, TAttributesType a_attributes, ShotGridEntityReference? a_parent)
			where TEntityType: ShotGridEntity
			where TAttributesType: notnull
		{
			ShotGridEntityCreateBaseData baseData = new ShotGridEntityCreateBaseData(a_projectId, a_parent);
			JObject baseDataToken = JObject.FromObject(baseData, Serializer);
			JObject encodedToken = JObject.FromObject(a_attributes, Serializer);
			encodedToken.Merge(baseDataToken);

			string requestBody = encodedToken.ToString();

			HttpResponseMessage response = await Create(ShotGridEntityName.FromType<TEntityType>(), requestBody);
			string responseBody = await response.Content.ReadAsStringAsync();

			return ParseResponse<TEntityType>(response.StatusCode, responseBody);
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityFilePublish>> CreateFilePublish(int a_projectId, int a_shotId, int a_versionId, ShotGridEntityFilePublish.FilePublishAttributes a_publishAttributes)
		{
			ShotGridEntityCreateBaseData baseData = new ShotGridEntityCreateBaseData(a_projectId, new ShotGridEntityReference(ShotGridEntityName.Shot, a_shotId));
			JObject baseDataToken = JObject.FromObject(baseData, Serializer);
			JObject encodedToken = JObject.FromObject(a_publishAttributes, Serializer);
			encodedToken.Merge(baseDataToken);

			string requestBody = encodedToken.ToString();

			HttpResponseMessage response = await Create(ShotGridEntityName.PublishedFile, requestBody);
			string responseBody = await response.Content.ReadAsStringAsync();

			return ParseResponse<ShotGridEntityFilePublish>(response.StatusCode, responseBody);
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityAttachment>> CreateFileAttachment(int a_projectId, int a_publishId, ShotGridEntityAttachment a_attachmentAttributes)
		{
			ShotGridEntityCreateBaseData baseData = new ShotGridEntityCreateBaseData(a_projectId, new ShotGridEntityReference(ShotGridEntityName.PublishedFile, a_publishId));
			JObject baseDataToken = JObject.FromObject(baseData, Serializer);
			JObject encodedToken = JObject.FromObject(a_attachmentAttributes, Serializer);
			encodedToken.Merge(baseDataToken);

			string requestBody = encodedToken.ToString();

			HttpResponseMessage response = await Create(ShotGridEntityName.Attachment, requestBody);
			string responseBody = await response.Content.ReadAsStringAsync();

			return ParseResponse<ShotGridEntityAttachment>(response.StatusCode, responseBody);
		}

		private async Task<HttpResponseMessage> Create(ShotGridEntityName a_entityType, string a_entityDataAsString)
		{
			if (m_authentication == null)
			{
				throw new ShotGridAPIException("No authentication requested");
			}

			HttpRequestMessage request = new HttpRequestMessage
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri($"{BaseUrl}entity/{a_entityType.SnakeCasePlural}"),
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
			ShotGridEntityName entityType = ShotGridEntityName.FromType<TEntityType>();

			if (m_authentication == null)
			{
				throw new ShotGridAPIException("No authentication requested");
			}

			string fullRequestAsString = JsonConvert.SerializeObject(a_propertiesToSet, SerializerSettings);

			HttpRequestMessage request = new HttpRequestMessage
			{
				Method = HttpMethod.Put,
				RequestUri = new Uri($"{BaseUrl}entity/{entityType.SnakeCasePlural}/{a_entityId}"),
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

		public async Task<ShotGridAPIResponseGeneric> UpdateEntityProperties(ShotGridEntityName a_shotGridEntityName, int a_entityId, Dictionary<string, object> a_propertiesToSet)
		{
			if (m_authentication == null)
			{
				throw new ShotGridAPIException("No authentication requested");
			}

			string fullRequestAsString = JsonConvert.SerializeObject(a_propertiesToSet, SerializerSettings);

			HttpRequestMessage request = new HttpRequestMessage
			{
				Method = HttpMethod.Put,
				RequestUri = new Uri($"{BaseUrl}entity/{a_shotGridEntityName.SnakeCasePlural}/{a_entityId}"),
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

			return ParseResponse(a_shotGridEntityName.ImplementedType, response.StatusCode, responseBody);
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

		public async Task<ShotGridAPIResponse<ShotGridEntityRelation?>> FindRelationByCode(ShotGridEntityName a_relationType, string a_code)
		{
			ShotGridAPIResponse<ShotGridEntityRelation[]> relations = await FindAndParse<ShotGridEntityRelation[]>(a_relationType, new ShotGridSimpleSearchFilter(), 
				new JsonAttributeFieldEnumerator<ShotGridEntityRelation.RelationAttributes>().Get(), null);
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

		public void StartAutoRefreshToken(Action a_onUserAuthenticationRequested)
		{
			StopAutoRefreshToken();
			StartAuthTokenRefreshTask();
			m_onRequestUserAuthentication = a_onUserAuthenticationRequested; 
		}

		public void StopAutoRefreshToken()
		{
			if (m_tokenRefreshTask != null)
			{
				m_cancellationTokenSource.Cancel();
				m_tokenRefreshTask = null;
			}
		}

		private void StartAuthTokenRefreshTask()
		{
			if (!m_cancellationTokenSource.TryReset())
			{
				m_cancellationTokenSource = new CancellationTokenSource();
			}

			m_tokenRefreshTask = Task.Delay(AuthTokenRefreshTime, m_cancellationTokenSource.Token).ContinueWith(RefreshLoginToken);
		}

		private async void RefreshLoginToken(Task a_task)
		{
			ShotGridLoginResponse response = await TryRefreshToken();
			if (response.Success)
			{
				StartAuthTokenRefreshTask();
			}
			else
			{
				if (m_onRequestUserAuthentication == null)
				{
					throw new Exception("No user authentication callback installed.");
				}

				StopAutoRefreshToken();

				m_onRequestUserAuthentication.Invoke();
			}
		}

		public async Task<ShotGridAPIResponse<ShotGridEntityActivityStreamResponse>> GetProjectActivityStream(int a_targetProjectId)
		{
			HttpResponseMessage result = await SendApiRequest(new Uri($"{BaseUrl}entity/{ShotGridEntityName.Project.SnakeCasePlural}/{a_targetProjectId}/activity_stream"), HttpMethod.Get, new ByteArrayContent(Array.Empty<byte>()));
			string resultBody = await result.Content.ReadAsStringAsync();
			return ParseResponse<ShotGridEntityActivityStreamResponse>(result.StatusCode, resultBody);
		}
	}

	public class ShotGridEntityFieldSchema
	{
	};
}