using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace BasicKube.Api.Common.Components.ActionResultExtensions
{
    /// <summary>
    /// copy from aspnet core
    /// </summary>
    internal class ResponseContentTypeHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="actionResultContentType">用户设置的返回类型</param>
        /// <param name="httpResponseContentType">httpResponse中的返回类型</param>
        /// <param name="defaultContentType">默认返回类型</param>
        /// <param name="resolvedContentType"></param>
        /// <param name="resolvedContentTypeEncoding"></param>
        public static void ResolveContentTypeAndEncoding(
            string actionResultContentType,
            string httpResponseContentType,
            string defaultContentType,
            out string resolvedContentType,
            out Encoding resolvedContentTypeEncoding)
        {
            Debug.Assert(defaultContentType != null);

            var defaultContentTypeEncoding = MediaType.GetEncoding(defaultContentType);
            Debug.Assert(defaultContentTypeEncoding != null);

            // 1. User sets the ContentType property on the action result
            if (actionResultContentType != null)
            {
                resolvedContentType = actionResultContentType;
                var actionResultEncoding =
                    MediaType.GetEncoding(actionResultContentType);
                resolvedContentTypeEncoding =
                    actionResultEncoding ?? defaultContentTypeEncoding;
                return;
            }

            // 2. User sets the ContentType property on the http response directly
            if (!string.IsNullOrEmpty(httpResponseContentType))
            {
                var mediaTypeEncoding = MediaType.GetEncoding(httpResponseContentType);
                if (mediaTypeEncoding != null)
                {
                    resolvedContentType = httpResponseContentType;
                    resolvedContentTypeEncoding = mediaTypeEncoding;
                }
                else
                {
                    resolvedContentType = httpResponseContentType;
                    resolvedContentTypeEncoding = defaultContentTypeEncoding;
                }

                return;
            }

            // 3. Fall-back to the default content type
            resolvedContentType = defaultContentType;
            resolvedContentTypeEncoding = defaultContentTypeEncoding;
        }
    }

    public class ApiResultExecutor<T> : IActionResultExecutor<ApiResult<T>>
    {
        private static readonly string DefaultContentType
            = new MediaTypeHeaderValue("application/json")
            {
                Encoding = Encoding.UTF8
            }.ToString();

        private readonly IHttpResponseStreamWriterFactory _writerFactory;

        private readonly JsonSerializerOptions _jsonSerializerOptions;


        public ApiResultExecutor(
            IHttpResponseStreamWriterFactory writerFactory,
            IOptions<JsonOptions> jsonOptions)
        {
            _writerFactory = writerFactory;
            _jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions;
        }

        public async Task ExecuteAsync(ActionContext context, ApiResult<T> result)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(result);


            var response = context.HttpContext.Response;

            if (result.StatusCode != null)
            {
                response.StatusCode = result.StatusCode.Value;
            }

            ResponseContentTypeHelper
                .ResolveContentTypeAndEncoding(
                    result.ContentType,
                    response.ContentType,
                    DefaultContentType,
                    out var resolvedContentType,
                    out var resolvedContentTypeEncoding);

            response.ContentType = resolvedContentType; //DefaultContentType;

            await using var writer = _writerFactory
                .CreateWriter(response.Body, resolvedContentTypeEncoding);


            string valueString = null;

            #region newtonJSon

            //var serializerSettings = new JsonSerializerSettings()
            //{
            //    Converters = new List<JsonConverter>()
            //    {
            //        new PointConverter(),
            //        new JsonNetDateTimeConvert()
            //    },
            //    NullValueHandling = NullValueHandling.Ignore
            //};

            ////using var jsonWriter = new JsonTextWriter(writer)
            ////{
            ////    CloseOutput = false, 
            ////    AutoCompleteOnClose = false
            ////};

            ////var jsonSerializer = JsonSerializer.Create(serializerSettings);
            ////await Task.Factory.StartNew(() => jsonSerializer.Serialize(jsonWriter, result));

            //var valueString =
            //    await Task.Factory.StartNew(() => JsonConvert.SerializeObject(result, serializerSettings));


            #endregion

            #region System.Text.Json

            valueString = JsonSerializer
                .Serialize(result, _jsonSerializerOptions);

            #endregion

            await writer.WriteAsync(valueString);
            await writer.FlushAsync();
        }
    }
}