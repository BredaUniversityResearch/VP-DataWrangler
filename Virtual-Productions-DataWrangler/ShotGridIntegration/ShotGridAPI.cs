using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using DataApiCommon;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ShotGridIntegration
{
	public class ShotGridAPI: DataApi
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
				try
				{
					ShotGridErrorResponse? errorResponse = JsonConvert.DeserializeObject<ShotGridErrorResponse>(responseString, SerializerSettings);

					return new ShotGridLoginResponse(false, errorResponse, null);
				}
				catch(JsonException ex)
				{
					return new ShotGridLoginResponse(false, new ShotGridErrorResponse()
					{
						Errors = new ShotGridErrorResponse.RequestError[]
						{
							new() {Detail = $"Exception occurred: {ex.Message}. Full response: {responseString}", HttpStatus = (int)responseAuth.StatusCode, Title = "Unknown Error"}
						}
					}, null);
				}
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

		public void SetInvalidAuthentication()
		{
			m_authentication = new ShotGridAuthentication(new APIAuthResponse() { AccessToken = "INVALID", IdToken = "INVALID", RefreshToken = "INVALID", TokenType = "INVALID"});
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

		public override async Task<DataApiResponse<DataEntityProject[]>> GetActiveProjects()
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			filter.FieldIs("sg_status", "Active");
			return await RemoteFindParseAndConvert<DataEntityProject[]>(ShotGridEntityTypeInfo.Project, filter,
				JsonAttributeFieldEnumerator.Get<ShotGridEntityProject.ProjectAttributes>(), null);
		}

		public override async Task<DataApiResponse<DataEntityShot[]>> GetShotsForProject(int a_projectId)
		{
			ShotGridSimpleSearchFilter filter = ShotGridSimpleSearchFilter.ForProject(a_projectId);
			return await RemoteFindParseAndConvert<DataEntityShot[]>(ShotGridEntityTypeInfo.Shot, filter,
				JsonAttributeFieldEnumerator.Get<ShotGridEntityShotAttributes>(), null);
		}

		public override async Task<DataApiResponse<DataEntityShotVersion[]>> GetVersionsForShot(int a_shotId)
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			filter.FieldIs("entity.Shot.id", a_shotId);
			return await RemoteFindParseAndConvert<DataEntityShotVersion[]>(ShotGridEntityTypeInfo.ShotVersion, filter, 
				JsonAttributeFieldEnumerator.Get<ShotVersionAttributes>(), null);
		}

		public async Task<DataApiResponse<DataEntityShotVersion>> FindShotVersionById(int a_shotVersionId)
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			filter.FieldIs("id", a_shotVersionId);
			DataApiResponse<DataEntityShotVersion[]> response = await RemoteFindParseAndConvert<DataEntityShotVersion[]>(ShotGridEntityTypeInfo.ShotVersion, filter,
				JsonAttributeFieldEnumerator.Get<ShotVersionAttributes>(), null);
			return new DataApiResponse<DataEntityShotVersion>(response.IsError ? null : response.ResultData[0], response.ErrorInfo);
		}

		public async Task<DataApiResponse<DataEntityFilePublish[]>> GetPublishesForShotVersion(int a_shotVersionId)
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			filter.FieldIs("version.Version.id", a_shotVersionId);
			return await RemoteFindParseAndConvert<DataEntityFilePublish[]>(ShotGridEntityTypeInfo.PublishedFile, filter, 
				JsonAttributeFieldEnumerator.Get<ShotGridEntityFilePublish.FilePublishAttributes>(), null);
		}

		public override async Task<DataApiResponse<DataEntityPublishedFileType[]>> GetPublishedFileTypes()
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			return await RemoteFindParseAndConvert<DataEntityPublishedFileType[]>(ShotGridEntityTypeInfo.PublishedFileType, filter, 
				JsonAttributeFieldEnumerator.Get<ShotGridEntityRelation.RelationAttributes>(), null);

		}

		public override async Task<DataApiResponse<DataEntityLocalStorage[]>> GetLocalStorages()
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			return await RemoteFindParseAndConvert<DataEntityLocalStorage[]>(ShotGridEntityTypeInfo.LocalStorage, filter,
				JsonAttributeFieldEnumerator.Get<ShotGridEntityLocalStorage.LocalStorageAttributes>(), null);
		}

		//public async Task<DataApiResponse<ShotGridEntityRelation[]>> GetRelations(ShotGridEntityTypeInfo a_relationType)
		//{
		//	ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
		//	return await RemoteFindParseAndConvert<ShotGridEntityRelation[]>(a_relationType, filter,
		//		JsonAttributeFieldEnumerator.Get<ShotGridEntityRelation.RelationAttributes>(), null);
		//}

		private bool ParseResponse(HttpStatusCode a_statusCode, string a_responseString, [NotNullWhen(true)] out JToken? a_successData, [NotNullWhen(false)] out DataApiErrorDetails? a_error)
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
					a_error = new DataApiErrorDetails
					{
						Errors = new DataApiErrorDetails.RequestError[]
						{
							new() {Description = "Invalid response returned from ShotGrid, missing 'data' node in root Json object"}
						}
					};
				}

			}
			else
			{
				ShotGridErrorResponse? response = parsedResult.ToObject<ShotGridErrorResponse>();
				if (response == null)
				{
					throw new Exception("Error response but could not deserialize error response data");
				}

				a_error = new DataApiErrorDetails();
				a_error.Errors = new DataApiErrorDetails.RequestError[response.Errors.Length];
				for( int i = 0; i < response.Errors.Length; ++i)
				{
					a_error.Errors[i] = new DataApiErrorDetails.RequestError() {Description = response.Errors[i].Title};
				}
			}

			if (a_error != null)
			{
				ReportError(a_error);
			}

			return a_successData != null;
		}

		private ShotGridAPIResponseGeneric ParseResponse(Type a_typeToParse, HttpStatusCode a_statusCode, string a_responseString)
		{
			object? returnValue = null;
			if (ParseResponse(a_statusCode, a_responseString, out JToken? dataData, out DataApiErrorDetails? requestError))
			{
				returnValue = dataData.ToObject(a_typeToParse, Serializer);

				if (returnValue == null)
				{
					throw new Exception($"Failed to deserialized data. Target type: {a_typeToParse}. Data: {dataData}");
				}
			}
			return new ShotGridAPIResponseGeneric(returnValue, requestError);
		}

		private void ReportError(DataApiErrorDetails a_errorResponse)
		{
			//Todo: Add logging here.
			throw new Exception(a_errorResponse.ToString());
		}

		private async Task<ShotGridAPIResponseGeneric> RemoteFindAndParse(ShotGridEntityTypeInfo a_entityType, bool a_entityIsArray, ShotGridSimpleSearchFilter a_filters, string[] a_fields, ShotGridSortSpecifier? a_sort)
		{
			HttpResponseMessage result = await RemoteFind(a_entityType, a_filters, a_fields, a_sort);
			string resultBody = await result.Content.ReadAsStringAsync();
			Type typeToParse = a_entityType.ImplementedType;
			if (a_entityIsArray)
			{
				typeToParse = a_entityType.ImplementedType.MakeArrayType();
			}
			return ParseResponse(typeToParse, result.StatusCode, resultBody);
		}

		private async Task<DataApiResponse<TTargetType>> RemoteFindParseAndConvert<TTargetType>(ShotGridEntityTypeInfo a_entityType, ShotGridSimpleSearchFilter a_filters, string[] a_fields, ShotGridSortSpecifier? a_sort)
			where TTargetType : class
		{
			ShotGridAPIResponseGeneric parsedResponse = await RemoteFindAndParse(a_entityType, typeof(TTargetType).IsArray, a_filters, a_fields, a_sort);
			return ConvertResponse<TTargetType>(parsedResponse);
		}

		private DataApiResponse<TTargetType> ConvertResponse<TTargetType>(ShotGridAPIResponseGeneric a_parsedResponse)
			where TTargetType: class
		{
			DataApiResponseGeneric genericResponse = ConvertResponse(a_parsedResponse, typeof(TTargetType));
			return new DataApiResponse<TTargetType>(genericResponse);
		}

		private DataApiResponseGeneric ConvertResponse(ShotGridAPIResponseGeneric a_response, Type a_targetConversionDataEntityType)
		{
			if (a_response.IsError)
			{
				return new DataApiResponseGeneric(null, a_response.ErrorInfo);
			}

			object? result = null;
			if (a_targetConversionDataEntityType.IsArray)
			{
				int arrayIndex = 0;
				ICollection<ShotGridEntity> collection = (ICollection<ShotGridEntity>)a_response.ResultDataGeneric;
				Array resultArray = Array.CreateInstance(a_targetConversionDataEntityType.GetElementType()!, collection.Count);
				foreach (ShotGridEntity ent in collection)
				{
					resultArray.SetValue(ent.ToDataEntity(), arrayIndex);
					++arrayIndex;
				}

				result = resultArray;
			}
			else
			{
				result = ((ShotGridEntity)a_response.ResultDataGeneric).ToDataEntity();
			}

			return new DataApiResponseGeneric(result, a_response.ErrorInfo);
		}

		private async Task<HttpResponseMessage> RemoteFind(ShotGridEntityTypeInfo a_entityType, ShotGridSimpleSearchFilter a_filters, string[] a_fields, ShotGridSortSpecifier? a_sortSpecifier)
		{
			ShotGridDataRequestQuery query = new ShotGridDataRequestQuery(a_filters, a_fields, a_sortSpecifier);
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

		public override async Task<DataApiResponse<DataEntityShot>> CreateNewShot(int a_projectId, DataEntityShot a_entityShot)
		{
			ShotGridEntityShot shot = new ShotGridEntityShot(a_entityShot);
			return await CreateNewEntity<ShotGridEntityShot, ShotGridEntityShotAttributes, DataEntityShot>(a_projectId, shot.Attributes, null);
		}

		public override async Task<DataApiResponse<DataEntityShotVersion>> CreateNewShotVersion(int a_projectId, int a_parentShotId, DataEntityShotVersion a_versionData)
		{
			ShotGridEntityShotVersion shotVersion = new ShotGridEntityShotVersion(a_versionData);
			return await CreateNewEntity<ShotGridEntityShotVersion, ShotVersionAttributes, DataEntityShotVersion>(a_projectId, shotVersion.Attributes, new ShotGridEntityReference(ShotGridEntityTypeInfo.Shot, a_parentShotId));
		}

		public async Task<DataApiResponse<TResultEntityType>> CreateNewEntity<TShotGridEntityType, TAttributesType, TResultEntityType>(int a_projectId, TAttributesType a_attributes, ShotGridEntityReference? a_parent)
			where TShotGridEntityType : ShotGridEntity
			where TResultEntityType: DataEntityBase
			where TAttributesType: notnull
		{
			ShotGridEntityCreateBaseData baseData = new ShotGridEntityCreateBaseData(a_projectId, a_parent);
			JObject baseDataToken = JObject.FromObject(baseData, Serializer);
			JObject encodedToken = JObject.FromObject(a_attributes, Serializer);
			encodedToken.Merge(baseDataToken);

			string requestBody = encodedToken.ToString();

			ShotGridEntityTypeInfo entityTypeInfo = ShotGridEntityTypeInfo.FromType<TShotGridEntityType>();
			HttpResponseMessage response = await Create(entityTypeInfo, requestBody);
			string responseBody = await response.Content.ReadAsStringAsync();

			ShotGridAPIResponseGeneric apiResponse = ParseResponse(entityTypeInfo.ImplementedType, response.StatusCode, responseBody);
			return ConvertResponse<TResultEntityType>(apiResponse);
		}

		public override async Task<DataApiResponse<DataEntityFilePublish>> CreateFilePublish(int a_projectId, int a_shotId, int a_versionId, DataEntityFilePublish a_publishAttributes)
		{
			if (a_publishAttributes.ShotVersion != null && a_publishAttributes.ShotVersion.EntityId != a_versionId)
			{
				throw new ShotGridAPIException("Creating file publish with a shot version link which does not match the supplied version id.");
			}

			ShotGridEntityFilePublish attrib = new ShotGridEntityFilePublish(a_publishAttributes);
			attrib.Attributes.ShotVersion = new ShotGridEntityReference(ShotGridEntityTypeInfo.ShotVersion, a_versionId);
			DataApiResponse<DataEntityFilePublish> response = await CreateNewEntity<ShotGridEntityFilePublish, ShotGridEntityFilePublish.FilePublishAttributes, DataEntityFilePublish>(a_projectId, attrib.Attributes, new ShotGridEntityReference(ShotGridEntityTypeInfo.Shot, a_shotId));
			return response;
		}

		//public async Task<ShotGridAPIResponse<ShotGridEntityAttachment>> CreateFileAttachment(int a_projectId, int a_publishId, ShotGridEntityAttachment a_attachmentAttributes)
		//{
		//	ShotGridEntityCreateBaseData baseData = new ShotGridEntityCreateBaseData(a_projectId, new ShotGridEntityReference(ShotGridEntityTypeInfo.PublishedFile, a_publishId));
		//	JObject baseDataToken = JObject.FromObject(baseData, Serializer);
		//	JObject encodedToken = JObject.FromObject(a_attachmentAttributes, Serializer);
		//	encodedToken.Merge(baseDataToken);

		//	string requestBody = encodedToken.ToString();

		//	HttpResponseMessage response = await Create(ShotGridEntityTypeInfo.Attachment, requestBody);
		//	string responseBody = await response.Content.ReadAsStringAsync();

		//	return ParseResponse<ShotGridEntityAttachment>(response.StatusCode, responseBody, (a_ent) => );
		//}

		private async Task<HttpResponseMessage> Create(ShotGridEntityTypeInfo a_entityType, string a_entityDataAsString)
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

		//public async Task<DataApiResponse<TDataEntityType>> UpdateShotGridEntityProperties<TDataEntityType>(int a_entityId, Dictionary<string, object> a_propertiesToSet)
		//	where TDataEntityType: DataEntityBase
		//{
		//	ShotGridEntityTypeInfo entityType = ShotGridEntityTypeInfo.FromDataEntityType(typeof(TDataEntityType));

		//	if (m_authentication == null)
		//	{
		//		throw new ShotGridAPIException("No authentication requested");
		//	}

		//	string fullRequestAsString = JsonConvert.SerializeObject(a_propertiesToSet, SerializerSettings);

		//	HttpRequestMessage request = new HttpRequestMessage
		//	{
		//		Method = HttpMethod.Put,
		//		RequestUri = new Uri($"{BaseUrl}entity/{entityType.SnakeCasePlural}/{a_entityId}"),
		//		Headers = {
		//			{ HttpRequestHeader.Accept.ToString(), "application/json" },
		//			{ HttpRequestHeader.AcceptEncoding.ToString(), "gzip, deflate, br" },
		//			{ HttpRequestHeader.Authorization.ToString(), "Bearer " + m_authentication.AccessToken },
		//			{ HttpRequestHeader.ContentType.ToString(), "application/vnd+shotgun.api3_array+json; charset=utf-8" },
		//		},
		//		Content = new ByteArrayContent(Encoding.UTF8.GetBytes(fullRequestAsString))
		//	};

		//	HttpResponseMessage response = await m_client.SendAsync(request);
		//	string responseBody = await response.Content.ReadAsStringAsync();

		//	ShotGridAPIResponseGeneric apiResponse = ParseResponse(entityType.ImplementedType, response.StatusCode, responseBody);
		//	return ConvertResponse<TDataEntityType>(apiResponse);
		//}

		public override async Task<DataApiResponseGeneric> UpdateEntityProperties(DataEntityBase a_entity, Dictionary<string, object> a_propertiesToSet)
		{
			if (m_authentication == null)
			{
				throw new ShotGridAPIException("No authentication requested");
			}

			ShotGridEntityTypeInfo typeInfo = ShotGridEntityTypeInfo.FromDataEntityType(a_entity.GetType());
			string fullRequestAsString = JsonConvert.SerializeObject(a_propertiesToSet, SerializerSettings);

			HttpRequestMessage request = new HttpRequestMessage
			{
				Method = HttpMethod.Put,
				RequestUri = new Uri($"{BaseUrl}entity/{typeInfo.SnakeCasePlural}/{a_entity.EntityId}"),
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

			ShotGridAPIResponseGeneric apiResponse = ParseResponse(typeInfo.ImplementedType, response.StatusCode, responseBody);
			return ConvertResponse(apiResponse, typeInfo.DataEntityType!);
		}

		public async Task<ShotGridAPIResponseGeneric> GetEntityFieldSchema(string a_entityType, int a_projectId)
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

			return ParseResponse(typeof(ShotGridEntityFieldSchema), response.StatusCode, responseBody);
		}

		public async Task<DataApiResponse<DataEntityPublishedFileType>> FindPublishedFileTypeByCode(string a_code)
		{
			ShotGridAPIResponseGeneric relations = await RemoteFindAndParse(ShotGridEntityTypeInfo.PublishedFileType, true, new ShotGridSimpleSearchFilter(), JsonAttributeFieldEnumerator.Get<ShotGridEntityRelation.RelationAttributes>(), null);
			if (!relations.IsError)
			{
				ShotGridEntityRelation[] typedRelations = (ShotGridEntityRelation[]) relations.ResultDataGeneric;
				foreach(ShotGridEntityRelation relation in typedRelations)
				{
					if (relation.Attributes.Code == a_code)
					{
						return new DataApiResponse<DataEntityPublishedFileType>(new DataEntityPublishedFileType() { 
							EntityId = relation.Id,
							FileType = relation.Attributes.Code
							}, null);
					}
				}

				return new DataApiResponse<DataEntityPublishedFileType>(null, new DataApiErrorDetails() {
					Errors = new []{
						new DataApiErrorDetails.RequestError { 
							Description = $"Could not find file type relation for code {a_code}"
						}
					}
					});
			}
			else
			{
				return new DataApiResponse<DataEntityPublishedFileType>(null, relations.ErrorInfo);
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
	}

	public class ShotGridEntityFieldSchema
	{
	};
}