using System.Diagnostics.CodeAnalysis;

namespace DataApiCommon;

public class DataApiResponseGeneric
{
	public object? ResultDataGeneric = null;
	public DataApiErrorDetails? ErrorInfo = null;

	[MemberNotNullWhen(false, nameof(ResultDataGeneric))]
	[MemberNotNullWhen(true, nameof(ErrorInfo))]
	public virtual bool IsError => ErrorInfo != null;

	public DataApiResponseGeneric(object? a_result, DataApiErrorDetails? a_errorResponse)
	{
		ResultDataGeneric = a_result;
		ErrorInfo = a_errorResponse;
	}
}

public class DataApiResponse<TSuccessDataType>: DataApiResponseGeneric
	where TSuccessDataType : class?
{
	public TSuccessDataType? ResultData => (TSuccessDataType?)ResultDataGeneric;

	[MemberNotNullWhen(false, nameof(ResultData))]
	public override bool IsError => base.IsError;

	public DataApiResponse(DataApiResponseGeneric a_genericResponse)
		: base(a_genericResponse.ResultDataGeneric, a_genericResponse.ErrorInfo)
	{
		if (ResultDataGeneric != null && !(ResultDataGeneric is TSuccessDataType))
		{
			throw new Exception($"Failed to cast type {a_genericResponse.GetType()} to type {typeof(TSuccessDataType)}");
		}
	}

	public DataApiResponse(TSuccessDataType? a_result, DataApiErrorDetails? a_errorResponse)
		: base(a_result, a_errorResponse)
	{
	}
}