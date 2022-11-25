namespace ShotGridIntegration
{
	public class ShotGridAuthentication
	{
		public readonly DateTime ExpiryTime;
		public readonly string AccessToken;
		public readonly string RefreshToken;

		public ShotGridAuthentication(APIAuthResponse a_authResponse)
		{
			ExpiryTime = a_authResponse.ExpiresAt;
			AccessToken = a_authResponse.AccessToken;
			RefreshToken = a_authResponse.RefreshToken;
		}
	}
}
