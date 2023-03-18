﻿using BasicKube.Api.Common;

namespace BasicKube.Api.Controllers;

[ApiController]
[Route($"/api/[controller]/[action]/{{{RouteConstants.IamId}}}")]
public abstract class KubeControllerBase : ControllerBase
{
    public string NsName => (string)HttpContext.Items[RouteConstants.NsName]!;
    
    public int IamId => (int)HttpContext.Items[RouteConstants.IamId]!;

    public string? EnvName
    {
        get
        {
            object? obj = HttpContext.Items[RouteConstants.Env];
            if (obj == null)
            {
                return null;
            }
            return (string)obj;
        }
    }

}