using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ShotGridIntegration
{
	public class AutodeskIdentityAuthenticator
	{
		//https://forge.autodesk.com/en/docs/oauth/v2/tutorials/get-3-legged-token/

		public delegate void OnAuthenticatedDelegate(APIAuthResponse a_authResponse);

		private const int CallbackServerPort = 34912;

		private const string BaseAuthUrl = "https://developer.api.autodesk.com/authentication/v2/authorize";
		private const string AuthResponseType = "code";
		private const string ClientID = "AmpyJQBzeLZWvlXThagxmEDADXg91aLJ";
		private const string AuthScope = "data:read data:write data:create";
		private static readonly string ClientSecret;
		private static readonly string AuthRedirectUri = $"http://localhost:{CallbackServerPort}/";

		private const string AuthorizeCallbackResponse = @"HTTP/1.1 200 OK
Server: DataWranglerAuthCallback
Keep-Alive: timeout=2, max=200
Connection: Keep-Alive
Transfer-Encoding: chunked
Content-Type: text/html

22
Authorization success!
";

		private TcpListener? m_callbackListener;

		static AutodeskIdentityAuthenticator()
		{
			IConfigurationRoot configRoot = new ConfigurationBuilder().AddUserSecrets(typeof(AutodeskIdentityAuthenticator).Assembly).Build();
			string? configuredClientSecret = configRoot.GetSection("AutodeskIdentityOAuthClientSecret").Value;
			if (configuredClientSecret == null)
			{
				throw new KeyNotFoundException("Configuration of client secrets is incomplete, missing \"AutodeskIdentityOAuthClientSecret\"");
			}

			ClientSecret = configuredClientSecret;
		}

		private AutodeskIdentityAuthenticator()
		{
			m_callbackListener = null;
		}

		private AutodeskIdentityAuthenticator(int a_callbackServerPort)
		{
			m_callbackListener = TcpListener.Create(a_callbackServerPort);
			m_callbackListener.Start();
		}

		public static async Task<ShotGridLoginResponse> StartUserLoginFlow()
		{
			AutodeskIdentityAuthenticator auth = new AutodeskIdentityAuthenticator(CallbackServerPort);
			auth.LaunchBrowserLoginPrompt();
			APIAuthResponse response = await auth.AwaitAuthorizationCallback();
			return new ShotGridLoginResponse(true, null, response);
		}

		public static async Task<ShotGridLoginResponse> StartRefreshTokenFlow(string a_refreshToken)
		{
			AutodeskIdentityAuthenticator auth = new AutodeskIdentityAuthenticator();
			APIAuthResponse? response = await auth.PostRefreshTokenRequest(a_refreshToken);
			return new ShotGridLoginResponse(true, null, response);
		}

		private async Task<APIAuthResponse> AwaitAuthorizationCallback()
		{
			if (m_callbackListener == null)
			{
				throw new Exception();
			}

			TcpClient client = await m_callbackListener.AcceptTcpClientAsync();
			NetworkStream stream = client.GetStream();
			StringBuilder ms = new StringBuilder(2048);
			byte[] readBuffer = new byte[1024];
			do
			{
				int bytesRead = stream.Read(readBuffer, 0, readBuffer.Length);
				ms.Append(Encoding.ASCII.GetString(readBuffer, 0, bytesRead));
			} while (stream.DataAvailable);

			stream.Write(Encoding.ASCII.GetBytes(AuthorizeCallbackResponse));
			stream.Flush();
			client.Dispose();

			string authorizeResponse = ms.ToString();
			string authCode = ParseResponseForCode(authorizeResponse);

			APIAuthResponse? authentication = await PostAuthenticationTokenRequest(authCode);
			if (authentication == null)
			{
				throw new Exception("Authentication failed");
			}

			return authentication;
		}

		private static string BuildAuthorizationUrl()
		{
			return $"{BaseAuthUrl}?response_type={AuthResponseType}&client_id={ClientID}&redirect_uri={HttpUtility.UrlEncode(AuthRedirectUri)}&scope={HttpUtility.UrlEncode(AuthScope)}";
		}

		private void LaunchBrowserLoginPrompt()
		{
			Process.Start(new ProcessStartInfo {FileName = BuildAuthorizationUrl(), UseShellExecute = true});
		}

		private string ParseResponseForCode(string a_fullHttpResponse)
		{
			int firstNewline = a_fullHttpResponse.IndexOfAny(new[] {'\r', '\n'});
			if (firstNewline == -1)
			{
				throw new Exception("Invalid response received");
			}

			//GET /?code=VdKo9cWSSxcD8vSdWab--VsMnORwLBdQrZIWmOyb&state= HTTP/1.1
			const string preamble = "GET";
			const string epilogue = "HTTP/1.1";
			const string urlStart = "/?";

			string getParam = a_fullHttpResponse.Substring(0, firstNewline);
			if (getParam.StartsWith(preamble) && getParam.EndsWith(epilogue))
			{
				string rawURL = getParam.Substring(preamble.Length, getParam.Length - preamble.Length - epilogue.Length).Trim(' ');
				if (!rawURL.StartsWith(urlStart))
				{
					throw new Exception($"Unexpected url received, expected to start with \"{urlStart}\", got \"{rawURL}\"");
				}

				NameValueCollection queryResult = HttpUtility.ParseQueryString(rawURL.Substring(urlStart.Length));

				string? code = queryResult["code"];
				if (code == null)
				{
					throw new Exception($"Failed to get code from response. Full get param: \"{getParam}\"");
				}

				return code;
			}
			else
			{
				throw new Exception($"Invalid response received. Expected preamble \"{preamble}\" and epilogue \"{epilogue}\". Got: \"{getParam}\"");
			}
		}

		private async Task<APIAuthResponse?> PostAuthenticationTokenRequest(string a_authCode)
		{
			Dictionary<string, string> bodyParams = new Dictionary<string, string>
			{
				{"grant_type", "authorization_code"},
				{"code", a_authCode},
				{"redirect_uri", AuthRedirectUri},
				{"scope", AuthScope}
			};
			return await PostAuthenticationRequest(bodyParams); 
		}

		private async Task<APIAuthResponse?> PostRefreshTokenRequest(string a_refreshToken)
		{
			Dictionary<string, string> bodyParams = new Dictionary<string, string>
			{
				{"grant_type", "refresh_token"},
				{"refresh_token", a_refreshToken},
				{"scope", AuthScope}
			};
			return await PostAuthenticationRequest(bodyParams);
		}

		private async Task<APIAuthResponse?> PostAuthenticationRequest(Dictionary<string, string> a_requestBodyParams)
		{
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://developer.api.autodesk.com/authentication/v2/token");
			request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
			request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientID}:{ClientSecret}")));

			request.Content = new FormUrlEncodedContent(a_requestBodyParams);

			using HttpClient client = new HttpClient();
			HttpResponseMessage response = await client.SendAsync(request);

			string body = await response.Content.ReadAsStringAsync();
			if (!response.IsSuccessStatusCode)
			{
				throw new Exception($"Authentication token request resulted in non-success HTTP code: {response.StatusCode}, body: {body}");
			}

			return ParseAuthenticationTokenResponse(body);
		}

		private APIAuthResponse? ParseAuthenticationTokenResponse(string a_authTokenResponseBody)
		{
			APIAuthResponse? response = JsonConvert.DeserializeObject<APIAuthResponse>(a_authTokenResponseBody);
			if (response != null)
			{
				return response;
			}
			else
			{
				throw new Exception($"Failed to deserialize response: {response}");
			}
		}
	}
}
