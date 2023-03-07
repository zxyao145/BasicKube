using BasicKube.Api.Domain;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;

namespace BasicKube.Api.Filters;

public class IamIdAndNsNameFilter : Attribute, IAsyncResourceFilter
{
    private readonly IamService _iamService;
    private readonly ILogger<IamIdAndNsNameFilter> _logger;
    public IamIdAndNsNameFilter(IamService iamService, ILogger<IamIdAndNsNameFilter> logger)
    {
        _iamService = iamService;
        _logger = logger;
    }


    public async Task OnResourceExecutionAsync(
        ResourceExecutingContext context
        , ResourceExecutionDelegate next)
    {
        //if (context.ActionDescriptor is ControllerActionDescriptor controllerDescriptor)
        //{
        //    var controllerName = controllerDescriptor.ControllerName;
        //}

        var iamId = (string?)context.HttpContext.Request.RouteValues["IamId"];
        if(iamId != null)
        {
            var nsName = _iamService.GetNsName(iamId);
            _logger.LogDebug("IamdId:{0},NsName:{1}", iamId, nsName);
            context.HttpContext.Items["IamId"] = Convert.ToInt32(iamId);
            context.HttpContext.Items["NsName"] = nsName;
            SetAppName(context);
        }

        await next();
    }

    private static void SetAppName(ResourceExecutingContext context)
    {
        var appName = (string?)context.HttpContext.Request.RouteValues["appName"];
        if (!string.IsNullOrEmpty(appName))
        {
            context.HttpContext.Items["AppName"] = appName;
        }
    }
}
