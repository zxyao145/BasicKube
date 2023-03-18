using BasicKube.Web.Services.Http;

namespace BasicKube.Web.Services;

public class SvcHttp
    : BasicKubeHttp<SvcGrpInfo, SvcDetails, SvcEditCommand>
{
    public SvcHttp(HttpClient httpClient, ILogger<SvcHttp> logger)
        : base(httpClient, logger)
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
    public IngHttp(HttpClient httpClient, ILogger<IngHttp> logger)
        : base(httpClient, logger)
    {
    }

    protected override string GetControllerName()
    {
        return "Ing";
    }
}