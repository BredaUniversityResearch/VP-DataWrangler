namespace ShotGridIntegration
{
	public class ShotGridLoginResponse
	{
		public readonly bool Success;
		public ShotGridErrorResponse? ErrorResponse;

		public ShotGridLoginResponse(bool a_success, ShotGridErrorResponse? a_errorResponse)
		{
			Success = a_success;
			ErrorResponse = a_errorResponse;
		}
	}
}
