using System.Text.Json;

namespace BasicKube.Web.Services.Http;

public class AccountHttp
{
    public HttpClient Client { get; private set; }
    private readonly ILogger<AccountHttp> _logger;

    public AccountHttp(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<AccountHttp> logger
        )
    {
        var baseHttp = configuration["BasicKube:HttpBase"];
        ArgumentNullException.ThrowIfNull(baseHttp, "BasicKube:HttpBase");
        Client = httpClient;
        Client.BaseAddress = new Uri(baseHttp);
        _logger = logger;
    }


    public async Task<bool> LoginAsync(AccountLogin accountLogin)
    {
        ArgumentNullException.ThrowIfNull(accountLogin, "accountLogin");
        var url = "/api/Account/Login";
        try
        {
            var jsonContent = HttpHelper.GetJsonContent(accountLogin);
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
                _logger.LogError("LoginAsync failed, StatusCode: {0}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("LoginAsync failed: {0}", e);
        }
        return false;
    }

    public async Task<bool> LogoutAsync()
    {
        var url = "/api/Account/Logout";
        try
        {
            using HttpResponseMessage response = await
                Client.PostAsync(url, null);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResult = JsonSerializer.Deserialize<ApiResultDto<bool?>>(jsonResponse);
                return apiResult?.Code == 0;
            }
            else
            {
                _logger.LogError("LogoutAsync failed, StatusCode: {0}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("LogoutAsync failed: {0}", e);
        }
        return false;
    }

    public async Task<UserProfileDto?> GetUserProfile()
    {
        var url = "/api/Account/UserProfile";
        try
        {
            using HttpResponseMessage response = await
                Client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResult = JsonSerializer
                    .Deserialize<ApiResultDto<UserProfileDto>>(jsonResponse, HttpHelper.JsonSerializerOptions);
                if (apiResult?.IsSuccess() ?? false)
                {
                    return apiResult.Data;
                }
                return null;
            }
            else
            {
                _logger.LogError("GetUserProfile failed, StatusCode: {0}", response.StatusCode);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("GetUserProfile failed: {0}", e);
        }
        return null;
    }
}
