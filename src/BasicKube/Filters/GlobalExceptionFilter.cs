using k8s.Autorest;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicKube.Api.Filters;

public class GlobalExceptionFilter : IAsyncExceptionFilter
{
    private ILogger<GlobalExceptionFilter> _logger;
    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }


    public Task OnExceptionAsync(ExceptionContext context)
    {
        if(context.Exception is HttpOperationException e)
        {
            _logger.LogWarning("k8s output error, code:{0}, content:{1}", e.Response.StatusCode, e.Response.Content);
        }
        return Task.CompletedTask;
    }
}
