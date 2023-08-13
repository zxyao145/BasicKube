using System.Runtime.CompilerServices;
using AntDesign;
using BasicKube.Web.Services.Http;

namespace BasicKube.Web.Services;

public static class KubeHttpClientExt
{
    public static IServiceCollection AddKubeHttpClient(this IServiceCollection services)
    {
        services.AddScoped<CookieHandler>();

        services.AddHttpClient<DeployHttp>()
            .AddHttpMessageHandler<CookieHandler>();
        services.AddHttpClient<DaemonSetHttp>()
            .AddHttpMessageHandler<CookieHandler>();
        services.AddHttpClient<ScalerHttp>()
            .AddHttpMessageHandler<CookieHandler>();
        services.AddHttpClient<EventsHttp>()
            .AddHttpMessageHandler<CookieHandler>();
        services.AddHttpClient<SvcHttp>()
            .AddHttpMessageHandler<CookieHandler>();
        services.AddHttpClient<PodHttp>()
            .AddHttpMessageHandler<CookieHandler>();
        services.AddHttpClient<IngHttp>()
            .AddHttpMessageHandler<CookieHandler>();

        services.AddHttpClient<JobHttp>()
            .AddHttpMessageHandler<CookieHandler>();
        services.AddHttpClient<CronJobHttp>()
            .AddHttpMessageHandler<CookieHandler>();

        services.AddHttpClient<AccountHttp>()
            .AddHttpMessageHandler<CookieHandler>();
        services.AddHttpClient<MetricHttp>()
            .AddHttpMessageHandler<CookieHandler>();

        services.AddScoped<KubeHttpClient>();
        return services;
    }

    public static async Task NoticeHttpError(
        this INotificationService notificationService,
        HttpResponseMessage response,
        string defaultErrorContent = "something error"
    )
    {
        var content = defaultErrorContent;
        var jsonResponse = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(jsonResponse))
        {
            content = jsonResponse;
        }
        _ = notificationService.Error(new NotificationConfig
        {
            Message = $"Http: {response.StatusCode} ({response.StatusCode.ToString("D")})",
            Description = content
        });
    }

    public static Task NoticeException(
        this INotificationService notificationService,
        Exception e,
        [CallerMemberName] string title = ""
    )
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            title = "Something error";
        }
        else
        {
            title = "ERROR: " + title;
        }
        return notificationService.Error(new NotificationConfig
        {
            Message = title,
            Description = e.Message
        });
    }
}