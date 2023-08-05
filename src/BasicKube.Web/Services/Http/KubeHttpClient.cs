namespace BasicKube.Web.Services;

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
        JobHttp jobHttp,
        CronJobHttp cronJobHttp
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
        CronJobHttp = cronJobHttp;
    }

    public DeployHttp DeployHttp { get; }
    public DaemonSetHttp DaemonSetHttp { get; }
    public SvcHttp SvcHttp { get; }
    public ScalerHttp ScalerHttp { get; }
    public EventsHttp EventsHttp { get; }
    public PodHttp PodHttp { get; }
    public IngHttp IngHttp { get; }
    public JobHttp JobHttp { get; }
    public CronJobHttp CronJobHttp { get; }
}
