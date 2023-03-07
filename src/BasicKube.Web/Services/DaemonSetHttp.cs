using BasicKube.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace BasicKube.Web.Services;

public class DaemonSetHttp
{
    public HttpClient Client { get; private set; }
    private readonly ILogger<DaemonSetHttp> _logger;

    public DaemonSetHttp(HttpClient httpClient, ILogger<DaemonSetHttp> logger)
    {
        httpClient.BaseAddress = new Uri("http://localhost:5125/api/DaemonSet/");
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

    public async Task<List<DaemonSetDetails>> GetDeployUnitList(
        int iamId,
        string appName,
        string? env = null
        )
    {
        var url = BuildUrlPrefix(iamId, appName, nameof(GetDeployUnitList));
        if (!string.IsNullOrWhiteSpace(env))
        {
            url += "?env=" + env;
        }
        try
        {
            var data = await Client
                .GetFromJsonAsync<ApiResultDto<List<DaemonSetDetails>>>(url);
            return data?.Data ?? new List<DaemonSetDetails>();
        }
        catch (Exception e)
        {
            _logger.LogError("GetAppDaemonSetList failed: {0}", e);
            return new List<DaemonSetDetails>();
        }
    }

    public async Task<bool> Create(
        int iamId,
        DaemonSetCreateCommand cmd)
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
                _logger.LogError("Create failed, StatusCode: {0}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Create failed: {0}", e);
        }

        return false;
    }

    public async Task<bool> Update(int iamId, DaemonSetCreateCommand cmd)
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
                _logger.LogError("DaemonSetScale failed, StatusCode: {0}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("DaemonSetScale failed: {0}", e);
        }

        return false;
    }

    public async Task<bool> Publish(
        int iamId,
        string appName,
        string deployName,
        AppPublishCommand cmd
        )
    {
        var json = JsonSerializer.Serialize(cmd);

        var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");

        string url = BuildUrlPrefix(iamId, appName, "Publish");
        try
        {
            using HttpResponseMessage response = await Client
                .PutAsync($"{url}/{deployName}", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResult = JsonSerializer.Deserialize<ApiResultDto<bool?>>(jsonResponse);
                return apiResult?.Code == 0;
            }
            else
            {
                _logger.LogError("Publish failed, StatusCode: {0}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Publish failed: {0}", e);
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
                _logger.LogError("Del failed, StatusCode: {0}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Del failed: {0}", e);
        }

        return false;
    }

    public async Task<DaemonSetCreateCommand?> Details(
        int iamId,
        string appName,
        string deployName
        )
    {
        try
        {
            string url = BuildUrlPrefix(iamId, appName, "Details");
            var data = await Client.GetFromJsonAsync<ApiResultDto<DaemonSetCreateCommand>>
                ($"{url}/{deployName}");
            return data?.Data;
        }
        catch (Exception e)
        {
            _logger.LogError("GetDaemonSetDetails failed: {0}", e);
            return null;
        }
    }
}