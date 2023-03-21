using BasicKube.Web.Services.Http;

namespace BasicKube.Web.Services;

public class DaemonSetHttp
    : BasicKubeAppHttp<DaemonSetGrpInfo, DaemonSetDetails, DaemonSetEditCommand>
{
    public DaemonSetHttp(IConfiguration configuration, HttpClient httpClient, ILogger<DaemonSetHttp> logger)
        :base(configuration, httpClient, logger)
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
    public DeployHttp(IConfiguration configuration, HttpClient httpClient, ILogger<DeployHttp> logger)
        : base(configuration, httpClient, logger)
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
    public JobHttp(IConfiguration configuration, HttpClient httpClient, ILogger<JobHttp> logger)
        : base(configuration, httpClient, logger)
    {
    }

    protected override string GetControllerName()
    {
        return "Job";
    }
}
