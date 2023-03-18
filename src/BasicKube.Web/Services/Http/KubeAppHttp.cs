using BasicKube.Web.Services.Http;

namespace BasicKube.Web.Services;

public class DaemonSetHttp
    : BasicKubeAppHttp<DaemonSetGrpInfo, DaemonSetDetails, DaemonSetEditCommand>
{
    public DaemonSetHttp(HttpClient httpClient, ILogger<DaemonSetHttp> logger)
        :base(httpClient, logger)
    {
    }

    protected override string GetControllerName()
    {
        return "DaemonSet";
    }
}

public class DeployHttp
    : BasicKubeAppHttp<DeployGrpInfo, DeployDetails, DeployEditCommand>
{
    public DeployHttp(HttpClient httpClient, ILogger<DeployHttp> logger)
        : base(httpClient, logger)
    {
    }

    protected override string GetControllerName()
    {
        return "Deploy";
    }
}

public class JobHttp
    : BasicKubeAppHttp<JobGrpInfo, JobDetails, JobEditCommand>
{
    public JobHttp(HttpClient httpClient, ILogger<JobHttp> logger)
        : base(httpClient, logger)
    {
    }

    protected override string GetControllerName()
    {
        return "Job";
    }
}
