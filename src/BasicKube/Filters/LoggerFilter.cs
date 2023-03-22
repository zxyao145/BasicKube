using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace AoAuth.Common.Filters;

public class LoggerFilter : IAsyncActionFilter
{
    private readonly ILogger<LoggerFilter> _logger;

    public LoggerFilter(ILogger<LoggerFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        string controllerName = context.HttpContext.Request.RouteValues["controller"]?.ToString() ?? "NAController";
        string actionName = context.HttpContext.Request.RouteValues["action"]?.ToString() ?? "NAAction";

        _logger.LogInformation("begin req:{0}.{1}, query params:{2}",
            controllerName, actionName, context.HttpContext.Request.QueryString);
        var sw = Stopwatch.StartNew();
        try
        {
            await next();
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogInformation("end req:{0}.{1}, duration:{2}, exp:{3}",
                controllerName, actionName, sw.Elapsed.Milliseconds, e);
            throw;
        }
        sw.Stop();
        _logger.LogInformation("end req:{0}.{1}, duration:{2}, without exp",
            controllerName, actionName, sw.Elapsed.Milliseconds);
    }
}
