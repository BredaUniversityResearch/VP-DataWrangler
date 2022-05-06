namespace DataWranglerCommonWPF.Login
{
	public interface ILoginCredentialProvider
	{
		public string OAuthRefreshToken { get; set; }
	}
}
