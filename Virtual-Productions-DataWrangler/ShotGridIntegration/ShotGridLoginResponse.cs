using System.Diagnostics.CodeAnalysis;

namespace ShotGridIntegration
{
	public class ShotGridLoginResponse
	{
		[MemberNotNullWhen(false, nameof(ErrorResponse))]
		public bool Success { get; }
		public ShotGridErrorResponse? ErrorResponse;

		public ShotGridLoginResponse(bool a_success, ShotGridErrorResponse? a_errorResponse)
		{
			Success= a_success;
			ErrorResponse = a_errorResponse;
		}
	}
}
