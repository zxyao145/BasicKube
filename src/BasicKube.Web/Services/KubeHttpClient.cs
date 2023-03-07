namespace BasicKube.Web.Services;

public class KubeHttpClient
{
    public KubeHttpClient(
        AppManagerHttp appManagerHttp,
        DeployHttp deployHttp,
        DaemonSetHttp daemonSetHttp,
        SvcHttp svcHttp,
        ScalerHttp scalerHttp,
        EventsHttp eventsHttp,
        PodHttp podHttp,
        IngHttp ingHttp
        )
    {
        AppManagerHttp = appManagerHttp;
        DeployHttp = deployHttp;
        DaemonSetHttp = daemonSetHttp;
        SvcHttp = svcHttp;
        ScalerHttp = scalerHttp;
        EventsHttp = eventsHttp;
        PodHttp = podHttp;
        IngHttp = ingHttp;
    }

    public AppManagerHttp AppManagerHttp { get; }
    public DeployHttp DeployHttp { get; }
    public DaemonSetHttp DaemonSetHttp { get; }
    public SvcHttp SvcHttp { get; }
    public ScalerHttp ScalerHttp { get; }
    public EventsHttp EventsHttp { get; }
    public PodHttp PodHttp { get; }
    public IngHttp IngHttp { get; }
}
