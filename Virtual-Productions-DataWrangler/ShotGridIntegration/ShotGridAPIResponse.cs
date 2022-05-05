using System.Diagnostics.CodeAnalysis;

namespace ShotGridIntegration
{
	public class ShotGridAPIResponse<TSuccessDataType>
		where TSuccessDataType: class
	{
		public TSuccessDataType? ResultData = null;
		public ShotGridErrorResponse? ErrorInfo = null;

		[MemberNotNullWhen(false, nameof(ResultData))]
		[MemberNotNullWhen(true, nameof(ErrorInfo))]
		public bool IsError => ErrorInfo != null;

		public ShotGridAPIResponse(TSuccessDataType? a_result, ShotGridErrorResponse? a_errorResponse)
		{
			ResultData = a_result;
			ErrorInfo = a_errorResponse;
		}
	}
}
