using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace BasicKube.Web.Services;

public class DeployHttp
{
    public HttpClient Client { get; private set; }
    private readonly ILogger<DeployHttp> _logger;

    public DeployHttp(HttpClient httpClient, ILogger<DeployHttp> logger)
    {
        httpClient.BaseAddress = new Uri("http://localhost:5125/api/Deploy/");
        Client = httpClient;
        _logger = logger;
    }

    private static string BuildUrlPrefix(
        int iamId,
        string appName,
        string actionName
        )
    {
        return $"{actionName}/{iamId}/{appName}";
    }

    public async Task<List<DeployDetails>> GetDeployUnitList(
        int iamId,
        string appName,
        string? env = null)
    {
        var url = BuildUrlPrefix( iamId, appName, nameof(GetDeployUnitList));
        if (!string.IsNullOrWhiteSpace(env))
        {
            url += "?env=" + env;
        }
        try
        {
            var data = await Client
                .GetFromJsonAsync<ApiResultDto<List<DeployDetails>>>(url);
            return data?.Data ?? new List<DeployDetails>();
        }
        catch (Exception e)
        {
            _logger.LogError("GetAppDeployList failed: {0}", e);
            return new List<DeployDetails>();
        }
    }

    public async Task<bool> Create(
        int iamId,
        DeployCreateCommand cmd)
    {
        var json = JsonSerializer.Serialize(cmd);

        var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");

        string url = BuildUrlPrefix(iamId, cmd.AppName, "Create");
        try
        {
            using HttpResponseMessage response = await Client
                .PostAsync(url, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResult = JsonSerializer.Deserialize<ApiResultDto<bool?>>(jsonResponse);
                return apiResult?.Code == 0;
            }
            else
            {
                _logger.LogError("DeployScale failed, StatusCode: {0}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("DeployScale failed: {0}", e);
        }

        return false;
    }

    public async Task<bool> Update(int iamId, DeployCreateCommand cmd)
    {
        var json = JsonSerializer.Serialize(cmd);

        var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");

        string url = BuildUrlPrefix(iamId, cmd.AppName, "Update");
        try
        {
            using HttpResponseMessage response = await Client
                .PostAsync(url, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResult = JsonSerializer.Deserialize<ApiResultDto<bool?>>(jsonResponse);
                return apiResult?.Code == 0;
            }
            else
            {
                _logger.LogError("DeployScale failed, StatusCode: {0}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("DeployScale failed: {0}", e);
        }

        return false;
    }

    public async Task<bool> Publish(
        int iamId,
        string appName,
        AppPublishCommand cmd
        )
    {
        var json = JsonSerializer.Serialize(cmd);

        var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");

        string url = BuildUrlPrefix(iamId, appName, "Publish");
        try
        {
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
                _logger.LogError("DeployScale failed, StatusCode: {0}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("DeployScale failed: {0}", e);
        }

        return false;
    }

    public async Task<bool> Del(
        int iamId,
        string appName,
        string deployName
        )
    {
        try
        {
            string url = BuildUrlPrefix(iamId, appName, "Del");

            using HttpResponseMessage response = await Client
                .DeleteAsync($"{url}/{deployName}");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var apiResult = JsonSerializer.Deserialize<ApiResultDto<bool?>>(jsonResponse);
                return apiResult?.Code == 0;
            }
            else
            {
                _logger.LogError("DeployScale failed, StatusCode: {0}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("DeployScale failed: {0}", e);
        }

        return false;
    }

    public async Task<DeployCreateCommand?> Details(
        int iamId,
        string appName,
        string deployName
        )
    {
        try
        {
            string url = BuildUrlPrefix(iamId, appName, "Details");
            var data = await Client.GetFromJsonAsync<ApiResultDto<DeployCreateCommand>>
                ($"{url}/{deployName}");
            return data?.Data;
        }
        catch (Exception e)
        {
            _logger.LogError("GetDeployDetails failed: {0}", e);
            return null;
        }
    }
}