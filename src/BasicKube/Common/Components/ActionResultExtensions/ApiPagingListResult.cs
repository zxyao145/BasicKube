namespace BasicKube.Api.Common.Components.ActionResultExtensions;

public class ApiListResult<T> : ApiResult<IList<T>>
{
    #region constructor

    public ApiListResult()
    {

    }

    /// <summary>
    /// 
    /// </summary>
    public ApiListResult(ApiResultCode apiResultCode = ApiResultCode.Success, int? statusCode = null) : base(apiResultCode, statusCode)
    {
    }

    public ApiListResult(ApiResultCode apiResultCode, IList<T> data, int? statusCode = null)
        : this(apiResultCode, statusCode)
    {
        Data = data;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <param name="apiResultCode"></param>
    /// <param name="statusCode"></param>
    public ApiListResult(IList<T> data, ApiResultCode apiResultCode = ApiResultCode.Success, int? statusCode = null)
        : this(apiResultCode, statusCode)
    {
        Data = data;
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="apiResultCode"></param>
    public ApiListResult(Exception exception, int code)
        : base(exception, code)
    {

    }

    #endregion

}

/// <summary>
/// ApiResult&lt;IList&lt;T&gt;&gt;
/// </summary>
/// <typeparam name="T"></typeparam>
public class ApiPagingListResult<T> : ApiResult<PageListDto<T>>
{
    #region constructor

    public ApiPagingListResult()
    {

    }

    /// <summary>
    /// 
    /// </summary>
    public ApiPagingListResult(ApiResultCode apiResultCode = ApiResultCode.Success, int? statusCode = null)
        : base(apiResultCode, statusCode)
    {
    }

    public ApiPagingListResult(ApiResultCode apiResultCode, PageListDto<T> data, int? statusCode = null)
        : this(apiResultCode, statusCode)
    {
        Data = data;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <param name="apiResultCode"></param>
    /// <param name="statusCode"></param>
    public ApiPagingListResult(PageListDto<T> data, ApiResultCode apiResultCode = ApiResultCode.Success, int? statusCode = null)
        : this(apiResultCode, statusCode)
    {
        Data = data;
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="apiResultCode"></param>
    public ApiPagingListResult(Exception exception, int code)
        : base(exception, code)
    {
        Data = PageListDto<T>.Empty;
    }

    #endregion
}
