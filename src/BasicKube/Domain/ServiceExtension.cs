using BasicKube.Api.Domain.App;
using BasicKube.Api.Domain.AppGroup;
using BasicKube.Api.Domain.Ing;
using BasicKube.Api.Domain.Pod;
using BasicKube.Api.Domain.Svc;
using BasicKube.Api.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using YamlDotNet.Core.Tokens;

namespace BasicKube.Api.Domain;

public static class ServiceExtension
{
    public static IServiceCollection AddBasicKubeServices(this IServiceCollection services)
    {
        services.ScanService();
        return services;
    }
}
