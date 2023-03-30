using AntDesign;
using BasicKube.Web.Services.Http;
using System.Runtime.CompilerServices;

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
        services.AddHttpClient<AccountHttp>()
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

public class KubeHttpClient
{
    public KubeHttpClient(
        DeployHttp deployHttp,
        DaemonSetHttp daemonSetHttp,
        SvcHttp svcHttp,
        ScalerHttp scalerHttp,
        EventsHttp eventsHttp,
        PodHttp podHttp,
        IngHttp ingHttp,
        JobHttp jobHttp
        )
    {
        DeployHttp = deployHttp;
        DaemonSetHttp = daemonSetHttp;
        SvcHttp = svcHttp;
        ScalerHttp = scalerHttp;
        EventsHttp = eventsHttp;
        PodHttp = podHttp;
        IngHttp = ingHttp;
        JobHttp = jobHttp;
    }

    public DeployHttp DeployHttp { get; }
    public DaemonSetHttp DaemonSetHttp { get; }
    public SvcHttp SvcHttp { get; }
    public ScalerHttp ScalerHttp { get; }
    public EventsHttp EventsHttp { get; }
    public PodHttp PodHttp { get; }
    public IngHttp IngHttp { get; }
    public JobHttp JobHttp { get; }
}
