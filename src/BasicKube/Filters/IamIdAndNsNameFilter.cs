using BasicKube.Api.Common;
using BasicKube.Api.Domain;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using System.Xml.Linq;

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
        var req = context.HttpContext.Request;
        var iamId = (string?)req.RouteValues[RouteConstants.IamId];
        if(iamId != null)
        {
            var nsName = _iamService.GetNsName(iamId);
            _logger.LogDebug("IamdId:{0},NsName:{1}", iamId, nsName);
            context.HttpContext.Items[RouteConstants.IamId] = Convert.ToInt32(iamId);
            context.HttpContext.Items[RouteConstants.NsName] = nsName;
            SetAppName(context);
        }
        StringValues env = req.Query[RouteConstants.Env];
        if (env.Count > 0)
        {
            context.HttpContext.Items[RouteConstants.Env] = env[0];
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
