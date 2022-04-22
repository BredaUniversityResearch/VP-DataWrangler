namespace ShotGridIntegration
{
	public class ShotGridLoginResponse
	{
		public readonly bool Success;
		public string? FailureReason;

		public ShotGridLoginResponse(bool a_success, string? a_failureReason)
		{
			Success = a_success;
			FailureReason = a_failureReason;
		}
	}
}
