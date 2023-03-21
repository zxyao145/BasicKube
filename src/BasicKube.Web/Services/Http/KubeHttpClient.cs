namespace BasicKube.Web.Services;

public static class KubeHttpClientExt
{
    public static IServiceCollection AddKubeHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient<DeployHttp>();
        services.AddHttpClient<DaemonSetHttp>();
        services.AddHttpClient<ScalerHttp>();
        services.AddHttpClient<EventsHttp>();
        services.AddHttpClient<SvcHttp>();
        services.AddHttpClient<PodHttp>();
        services.AddHttpClient<IngHttp>();
        services.AddHttpClient<JobHttp>();
        services.AddScoped<KubeHttpClient>();
        return services;
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
