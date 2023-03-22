using System;
using System.Collections.Generic;
using System.Text;

namespace BasicKube.Api.Common.Components.ActionResultExtensions
{
    /// <summary>
    /// 
    /// </summary>
    public enum ApiResultCode : int
    {
        /// <summary>
        /// 通用成功
        /// </summary>
        Success = 0,

        /// <summary>
        /// 通用失败
        /// </summary>
        Fail = 1,
    }


    public static class ApiResultCodeExt
    {
        public static int IntVal(this ApiResultCode apiResultCode)
        {
            return Convert.ToInt32(apiResultCode);
        }
    }
}
