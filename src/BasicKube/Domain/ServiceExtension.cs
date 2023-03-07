using BasicKube.Api.Domain.App;
using BasicKube.Api.Domain.Ing;
using BasicKube.Api.Domain.Pod;
using BasicKube.Api.Domain.Svc;
using Microsoft.Extensions.DependencyInjection;
using YamlDotNet.Core.Tokens;

namespace BasicKube.Api.Domain;

public static class ServiceExtension
{
    public delegate IAppService AppServiceResolver(string type);

    public static IServiceCollection AddBasicKubeServices(this IServiceCollection services)
    {
        services.AddScoped<DeployAppService>();
        services.AddScoped<DaemonSetAppService>();
        services.AddScoped<AppServiceResolver>(serviceProvider
            => type =>
        {
            return type.ToLower() switch
            {
                "deploy" => serviceProvider.GetRequiredService<DeployAppService>(),
                "daemonset" => serviceProvider.GetRequiredService<DaemonSetAppService>(),
                _ => throw new InvalidOperationException()
            };
        });


        services.AddScoped<IPodService,PodService>();
        services.AddScoped<IamService>();
        services.AddScoped<ISvcService, SvcService>();
        services.AddScoped<IIngService, IngService>();

        return services;
    }
}
