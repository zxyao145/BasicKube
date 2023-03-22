using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace BasicKube.Api.Filters;

public class LoggerFilter2 : IAsyncActionFilter
{
    private readonly ILogger<LoggerFilter2> _logger;

    public LoggerFilter2(ILogger<LoggerFilter2> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        string controllerName = context.HttpContext.Request.RouteValues["controller"]?.ToString() ?? "NAController";
        string actionName = context.HttpContext.Request.RouteValues["action"]?.ToString() ?? "NAAction";

        _logger.LogInformation("begin LoggerFilter2:{0}.{1}, query params:{2}",
            controllerName, actionName, context.HttpContext.Request.QueryString);
        var sw = Stopwatch.StartNew();
        try
        {
            await next();
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogInformation("end LoggerFilter2:{0}.{1}, duration:{2}, exp:{3}",
                controllerName, actionName, sw.Elapsed.Milliseconds, e);
            throw;
        }
        sw.Stop();
        _logger.LogInformation("end LoggerFilter2:{0}.{1}, duration:{2}, without exp",
            controllerName, actionName, sw.Elapsed.Milliseconds);
    }
}
