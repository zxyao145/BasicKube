using BasicKube.Web.Services.Http;

namespace BasicKube.Web.Services;

public class SvcHttp
    : BasicKubeHttp<SvcGrpInfo, SvcDetails, SvcEditCommand>
{
    public SvcHttp(IConfiguration configuration, HttpClient httpClient, ILogger<SvcHttp> logger)
        : base(configuration, httpClient, logger)
    {
    }

    protected override string GetControllerName()
    {
        return "Svc";
    }
}

public class IngHttp
    : BasicKubeHttp<IngGrpInfo, IngDetails, IngEditCommand>
{
    public IngHttp(IConfiguration configuration, HttpClient httpClient, ILogger<IngHttp> logger)
        : base(configuration, httpClient, logger)
    {
    }

    protected override string GetControllerName()
    {
        return "Ing";
    }
}