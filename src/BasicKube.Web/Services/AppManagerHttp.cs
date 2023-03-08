using System.Net.Http.Json;

namespace BasicKube.Web.Services
{
    public class AppManagerHttp
    {
        public HttpClient Client { get; private set; }
        private readonly ILogger<AppManagerHttp> _logger;

        public AppManagerHttp(HttpClient httpClient, ILogger<AppManagerHttp> logger)
        {
            httpClient.BaseAddress = new Uri("http://localhost:5125/api/AppManager/");
            Client = httpClient;
            _logger = logger;
        }

        public async Task<List<AppInfo>> GetDeployAppList(int iamId)
        {
            try
            {
                var data = await Client.GetFromJsonAsync<ApiResultDto<List<AppInfo>>>
                    ($"DeployGrpList/{iamId}");
                return data?.Data ?? new List<AppInfo>();
            }
            catch (Exception e)
            {
                _logger.LogError("DeployAppList failed: {0}", e);
                return new List<AppInfo>();
            }
        }

        public async Task<List<DaemonSetAppInfo>> GetDaemonSetAppList(int iamId)
        {
            try
            {
                var data = await Client.GetFromJsonAsync<ApiResultDto<List<DaemonSetAppInfo>>>
                    ($"DaemonSetGrpList/{iamId}");
                return data?.Data ?? new List<DaemonSetAppInfo>();
            }
            catch (Exception e)
            {
                _logger.LogError("DaemonSetAppList failed: {0}", e);
                return new List<DaemonSetAppInfo>();
            }
        }
    }
}