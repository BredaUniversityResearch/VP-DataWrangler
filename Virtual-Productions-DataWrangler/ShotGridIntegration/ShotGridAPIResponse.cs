using System.Diagnostics.CodeAnalysis;

namespace ShotGridIntegration
{
	public class ShotGridAPIResponseGeneric
	{
		public object? ResultDataGeneric = null;
		public ShotGridErrorResponse? ErrorInfo = null;

		[MemberNotNullWhen(false, nameof(ResultDataGeneric))]
		[MemberNotNullWhen(true, nameof(ErrorInfo))]
		public virtual bool IsError => ErrorInfo != null;

		public ShotGridAPIResponseGeneric(object? a_result, ShotGridErrorResponse? a_errorResponse)
		{
			ResultDataGeneric = a_result;
			ErrorInfo = a_errorResponse;
		}
	}

	public class ShotGridAPIResponse<TSuccessDataType>: ShotGridAPIResponseGeneric
		where TSuccessDataType: class?
	{
		public TSuccessDataType? ResultData => (TSuccessDataType?)ResultDataGeneric;

		[MemberNotNullWhen(false, nameof(ResultData))]
		public override bool IsError => base.IsError;

		public ShotGridAPIResponse(ShotGridAPIResponseGeneric a_genericResponse)
			: base(a_genericResponse.ResultDataGeneric, a_genericResponse.ErrorInfo)
		{
			if (!(ResultDataGeneric is TSuccessDataType))
			{
				throw new Exception($"Failed to cast type {a_genericResponse.GetType()} to type {typeof(TSuccessDataType)}");
			}
		}

		public ShotGridAPIResponse(TSuccessDataType? a_result, ShotGridErrorResponse? a_errorResponse)
			: base(a_result, a_errorResponse)
		{
		}
	}
}
