using AntDesign;
using System.Text.Json;

namespace BasicKube.Web.Services.Http;

public class AccountHttp
{
    public HttpClient Client { get; private set; }
    private readonly ILogger<AccountHttp> _logger;
    private readonly INotificationService _notificationService;

    public AccountHttp(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<AccountHttp> logger,
        INotificationService notificationService
        )
    {
        var baseHttp = configuration["BasicKube:HttpBase"];
        ArgumentNullException.ThrowIfNull(baseHttp, "BasicKube:HttpBase");
        Client = httpClient;
        Client.BaseAddress = new Uri(baseHttp);
        _logger = logger;
        _notificationService = notificationService;
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
                await _notificationService.NoticeHttpError(
                    response,
                    "login error"
                    );
            }
        }
        catch (Exception e)
        {
            _ = _notificationService.NoticeException(e);
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
                await _notificationService.NoticeHttpError(
                     response,
                     "logout error"
                     );
            }
        }
        catch (Exception e)
        {
            _ = _notificationService.NoticeException(e);
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
                await _notificationService.NoticeHttpError(
                     response,
                     "GetUserProfile error"
                     );
            }
        }
        catch (Exception e)
        {
            _ = _notificationService.NoticeException(e);
        }
        return null;
    }

    public async Task<List<IamNodeInfo>> GetUserIamInfo()
    {
        var url = "/api/Account/UserIamInfo";
        try
        {
            using HttpResponseMessage response = await
                Client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResult = JsonSerializer
                    .Deserialize<ApiResultDto<List<IamNodeInfo>>>(jsonResponse, HttpHelper.JsonSerializerOptions);
                if (apiResult?.IsSuccess() ?? false)
                {
                    return apiResult.Data ?? new List<IamNodeInfo>();
                }
                return new List<IamNodeInfo>();
            }
            else
            {
                await _notificationService.NoticeHttpError(
                     response,
                     "GetUserIamInfo error"
                     );
            }
        }
        catch (Exception e)
        {
            _ = _notificationService.NoticeException(e);
        }
        return new List<IamNodeInfo>();
    }

}
