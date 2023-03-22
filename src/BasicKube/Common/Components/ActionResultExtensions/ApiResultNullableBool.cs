using System;

namespace BasicKube.Api.Common.Components.ActionResultExtensions
{
    /// <summary>
    /// ApiResult<bool?>的实现
    /// </summary>
    public class ApiResultNullableBool : ApiResult<bool?>
    {
        public ApiResultNullableBool()
        {
            Data = null;
        }

        /// <summary>
        /// 
        /// </summary>
        public ApiResultNullableBool(ApiResultCode apiResultCode = ApiResultCode.Success)
            : base(apiResultCode)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="exception"></param>
        public ApiResultNullableBool(int code, string msg)
            : base(code, msg)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="exception"></param>
        public ApiResultNullableBool(int code, Exception exception)
            : base(code, exception.Message)
        {
        }
    }
}