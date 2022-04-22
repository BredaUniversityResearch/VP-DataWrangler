using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShotGridIntegration
{
	internal class ShotGridAuthentication
	{
		public readonly DateTime ExpiryTime;
		public readonly string AccessToken;
		public readonly string RefreshToken;

		public ShotGridAuthentication(APIAuthResponse a_authResponse)
		{
			ExpiryTime = DateTime.UtcNow + TimeSpan.FromSeconds(a_authResponse.expires_in);
			AccessToken = a_authResponse.access_token;
			RefreshToken = a_authResponse.refresh_token;
		}
	}
}
