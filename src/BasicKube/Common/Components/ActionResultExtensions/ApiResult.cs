using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
// using Newtonsoft.Json;

namespace BasicKube.Api.Common.Components.ActionResultExtensions
{
    public interface IApiResult
    {
        /// <summary>
        /// 状态码值
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 状态码含义
        /// </summary>
        public string Msg { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ApiResult<T> : ActionResult, IStatusCodeActionResult, IApiResult
    {
        #region constructor

        public ApiResult()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public ApiResult(T data)
        {
            Data = data;
        }

        /// <summary>
        /// 
        /// </summary>
        public ApiResult(int code, int? statusCode = null)
        {
            _code = code;
            StatusCode = statusCode;
        }


        /// <summary>
        /// 
        /// </summary>
        public ApiResult(int code, string msg, int? statusCode = null)
        {
            _code = code;
            Msg = msg;
            StatusCode = statusCode;
        }

        /// <summary>
        /// 
        /// </summary>
        public ApiResult(ApiResultCode apiResultCode = ApiResultCode.Success, int? statusCode = null)
            : this(apiResultCode.IntVal(), statusCode) { }


        public ApiResult(int code, T data, int? statusCode = null)
            : this(code, statusCode)
        {
            Data = data;
        }

        public ApiResult(ApiResultCode apiResultCode, T data, int? statusCode = null)
            : this(apiResultCode.IntVal(), data, statusCode) { }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="apiResultCode"></param>
        public ApiResult(Exception exception, int code)
            : this(code, exception.Message)
        {
        }

        #endregion



        #region public property

        protected int _code = ApiResultCode.Success.IntVal();

        /// <summary>
        /// 
        /// </summary>
        public int Code
        {
            get
            {
                return _code;
            }
            set => _code = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public T Data { get; set; }



        /// <summary>
        /// 状态码含义
        /// </summary>
        public string Msg { get; set; } = ApiResult.DefaultMsg;

        #endregion


        /// <summary>
        /// Http Status Code，for custom the response status code
        /// </summary>
        [JsonIgnore]
        [XmlIgnore]
        public int? StatusCode { get; protected set; }

        /// <summary>
        /// response ContentType
        /// </summary>
        [JsonIgnore]
        [XmlIgnore]
        public string ContentType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var services = context.HttpContext.RequestServices;
            var executor = services.GetRequiredService<ApiResultExecutor<T>>();
            return executor.ExecuteAsync(context, this);
        }
    }
}