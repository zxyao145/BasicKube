using BasicKube.Web.Services.Http;
using System.Text.Json;

namespace BasicKube.Web.Services;

public class DaemonSetHttp
    : BasicKubeAppHttp<DaemonSetGrpInfo, DaemonSetDetails, DaemonSetEditCommand>
{
    public DaemonSetHttp(IConfiguration configuration, HttpClient httpClient, ILogger<DaemonSetHttp> logger)
        : base(configuration, httpClient, logger)
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

public class CronJobHttp
    : BasicKubeAppHttp<CronJobGrpInfo, CronJobDetails, CronJobEditCommand>
{
    public CronJobHttp(IConfiguration configuration, HttpClient httpClient, ILogger<CronJobHttp> logger)
        : base(configuration, httpClient, logger)
    {
    }

    protected override string GetControllerName()
    {
        return "CronJob";
    }

    public async Task<bool> Suspend(int iamId, CronJobSuspendCommand cmd)
    {
        string url = GetBaseUrl(iamId);
        try
        {
            var jsonContent = GetJsonContent(cmd);
            using HttpResponseMessage response = await Client
                .PutAsync($"{url}", jsonContent);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResult = JsonSerializer.Deserialize<ApiResultDto<bool?>>(jsonResponse);
                return apiResult?.Code == 0;
            }
            else
            {
                Logger.LogError("Suspend failed, StatusCode: {0}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            Logger.LogError("Suspend failed: {0}", e);
        }

        return false;
    }
}

