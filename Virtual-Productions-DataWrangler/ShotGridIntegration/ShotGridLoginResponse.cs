using System.Diagnostics.CodeAnalysis;

namespace ShotGridIntegration
{
	public class ShotGridLoginResponse
	{
		[MemberNotNullWhen(false, nameof(ErrorResponse)), MemberNotNullWhen(true, nameof(AuthResponse))]
		public bool Success { get; }
		public ShotGridErrorResponse? ErrorResponse;
		public APIAuthResponse? AuthResponse;

		public ShotGridLoginResponse(bool a_success, ShotGridErrorResponse? a_errorResponse, APIAuthResponse? a_authResponse)
		{
			Success= a_success;
			ErrorResponse = a_errorResponse;
			AuthResponse = a_authResponse;
		}
	}
}
