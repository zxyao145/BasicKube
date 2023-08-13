using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace BasicKube.Web.Services.Http;

public abstract class BasicKubeHttp<TGrpInfo, TDetails, TCmd>
    : IBasicKubeHttp<TGrpInfo, TDetails, TCmd>
    where TCmd : class, IIamModel
{
    protected string _controller;

    public HttpClient Client { get; private set; }
    protected readonly ILogger Logger;

    protected BasicKubeHttp(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger logger
        )
    {
        //var baseHttp = configuration
        //    .GetSection("BasicKube")
        //    .GetValue<string>("HttpBase");
        //var baseHttp = configuration
        //     .GetSection("BasicKube")
        //     .GetSection("HttpBase")
        //     .Get<string>();
        //var baseHttp = configuration["BasicKube:HttpBase"];
        var baseHttp = configuration["BasicKube:HttpBase"];
        ArgumentNullException.ThrowIfNull(baseHttp, "BasicKube:HttpBase");
        Client = httpClient;
        Client.BaseAddress = new Uri(baseHttp);
        _controller = GetControllerName();
        ArgumentNullException.ThrowIfNull(_controller);
        Logger = logger;
    }

    protected abstract string GetControllerName();

    protected string GetBaseUrl(int iamId, [CallerMemberName] string? action = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        var url = $"/api/{_controller}/{action}/{iamId}";
        //Console.WriteLine(url);
        return url;
    }

    public static StringContent GetJsonContent<T>(T data)
    {
        return HttpHelper.GetJsonContent(data);
    }

    public async Task<List<TGrpInfo>> ListGrp(int iamId)
    {
        try
        {
            var url = GetBaseUrl(iamId);
            var data = await Client.GetFromJsonAsync<ApiResultDto<List<TGrpInfo>>>(url);
            return data?.Data ?? new List<TGrpInfo>();
        }
        catch (Exception e)
        {
            Logger.LogError("ListGrp failed: {0}", e);
            return new List<TGrpInfo>();
        }
    }

    public async Task<List<TDetails>> List(int iamId, string? grpName = null, string? env = null)
    {
        var url = GetBaseUrl(iamId);
        if (grpName != null)
        {
            url += "/" + grpName;
        }

        if (env != null)
        {
            url += "?env=" + env;
        }
        try
        {
            var data = await Client
                .GetFromJsonAsync<ApiResultDto<List<TDetails>>>(url);
            return data?.Data ?? new List<TDetails>();
        }
        catch (Exception e)
        {
            Logger.LogError("List failed: {0}", e);
            return new List<TDetails>();
        }
    }


    public async Task<bool> Create(TCmd command)
    {
        var url = GetBaseUrl(command.IamId);
        try
        {
            var jsonContent = GetJsonContent(command);
            using HttpResponseMessage response = await
                Client.PostAsync(url, jsonContent);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResult = JsonSerializer.Deserialize<ApiResultDto<bool?>>(jsonResponse);
                return apiResult?.Code == 0;
            }
            else
            {
                Logger.LogError("Create failed, StatusCode: {0}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            Logger.LogError("Create failed: {0}", e);
        }

        return false;
    }

    public async Task<bool> Update(TCmd command)
    {
        var url = GetBaseUrl(command.IamId);
        try
        {
            var jsonContent = GetJsonContent(command);
            using HttpResponseMessage response = await
                Client.PostAsync(url, jsonContent);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResult = JsonSerializer.Deserialize<ApiResultDto<bool?>>(jsonResponse);
                return apiResult?.Code == 0;
            }
            else
            {
                Logger.LogError("Update failed, StatusCode: {0}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            Logger.LogError("Update failed: {0}", e);
        }

        return false;
    }

    public async Task<bool> Del(int iamId, string appName)
    {
        try
        {
            string url = GetBaseUrl(iamId) + "/" + appName;

            using HttpResponseMessage response =
                await Client.DeleteAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResult = JsonSerializer.Deserialize<ApiResultDto<bool?>>(jsonResponse);
                return apiResult?.Code == 0;
            }
            else
            {
                Logger.LogError("Del failed, StatusCode: {0}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            Logger.LogError("Del failed: {0}", e);
        }

        return false;
    }

    public async Task<TCmd?> Details(int iamId, string appName)
    {
        try
        {
            string url = GetBaseUrl(iamId) + "/" + appName;
            var data = await Client
                .GetFromJsonAsync<ApiResultDto<TCmd>>(url);
            return data?.Data;
        }
        catch (Exception e)
        {
            Logger.LogError("GetDeployDetails failed: {0}", e);
            return null;
        }
    }
}


public abstract class BasicKubeAppHttp<TGrpInfo, TDetails, TCmd>
    : BasicKubeHttp<TGrpInfo, TDetails, TCmd>,
    IBasicKubeAppHttp<TGrpInfo, TDetails, TCmd>
    where TCmd : class, IIamModel
{
    protected BasicKubeAppHttp(IConfiguration configuration, HttpClient httpClient, ILogger logger)
        : base(configuration, httpClient, logger)
    {
    }

    public async Task<bool> Publish(int iamId, AppPublishCommand cmd)
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
                Logger.LogError("Publish failed, StatusCode: {0}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            Logger.LogError("Publish failed: {0}", e);
        }

        return false;
    }
}