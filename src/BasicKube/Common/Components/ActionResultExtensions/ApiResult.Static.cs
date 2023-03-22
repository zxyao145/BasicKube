using System;
using System.Diagnostics.CodeAnalysis;
using BasicKube.Api.Exceptions;

namespace BasicKube.Api.Common.Components.ActionResultExtensions;

/// <summary>
/// 默认的ApiResult实现集合
/// </summary>
public class ApiResult
{
    public const string DefaultMsg = "OK";


    public static ApiResult<PageListDto<bool?>> EmptyListResult
        = new ApiPagingListResult<bool?>(PageListDto<bool?>.Empty);

    /// <summary>
    /// ApiResult&lt;bool&gt; 且 Data = null 的实现
    /// </summary>
    public static ApiResultNullableBool Success = new ApiResultNullableBool();

    /// <summary>
    /// ApiResult&lt;bool&gt; 且 Data = null 的实现
    /// </summary>
    public static ApiResultNullableBool Fail = new ApiResultNullableBool(ApiResultCode.Fail);

 
    #region BuildFail

    public static ApiResult<bool?> BuildFail([NotNull] string msg)
    {
        var res = new ApiResult<bool?>(ApiResultCode.Fail, data: null);
        res.Msg = msg;
        return res;
    }

    public static ApiResult<bool?> BuildFail([NotNull] Exception exp)
    {
        var res = new ApiResult<bool?>(ApiResultCode.Fail, data: null);
        res.Msg = exp.Message;
        return res;
    }

    public static ApiResult<string> BuildFail(int code, string msg = null)
    {
        var res = new ApiResult<string>(code, msg);
        return res;
    }

    public static ApiResult<string> BuildFail(int code, Exception exp)
    {
        var res = new ApiResult<string>(code, exp.Message);
        return res;
    }


    public static ApiResult<string> BuildFail(AppException appException, string msg = null)
    {
        var res = new ApiResult<string>(appException.Code, msg ?? appException.Message);
        return res;
    }

    #endregion


    public static ApiResult<bool?> BuildSuccess(bool? data = null)
    {
        return BuildSuccess<bool?>(data);
    }


    public static ApiResult<T> BuildSuccess<T>([NotNull] T data, string msg = DefaultMsg)
    {
        var res = new ApiResult<T>(ApiResultCode.Success, data: data);
        res.Msg = msg;
        return res;
    }


    public static ApiResult<string> BuildSuccess([NotNull] string data = DefaultMsg)
    {
        var res = new ApiResult<string>(ApiResultCode.Success, data: data);
        return res;
    }
}

