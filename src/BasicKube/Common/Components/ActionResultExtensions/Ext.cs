using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicKube.Api.Common.Components.ActionResultExtensions;

public static class Ext
{
    public static IServiceCollection AddApiResult(this IServiceCollection services)
    {
        services.AddScoped(typeof(ApiResultExecutor<>));
        return services;
    }
}
