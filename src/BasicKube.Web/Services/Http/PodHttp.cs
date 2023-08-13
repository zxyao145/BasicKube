using System.Text.Json;

namespace BasicKube.Web.Services
{
    public class PodHttp
    {
        public HttpClient Client { get; private set; }
        private readonly ILogger<PodHttp> _logger;

        public PodHttp(
            IConfiguration configuration,
            HttpClient httpClient, 
            ILogger<PodHttp> logger)
        {
            var baseHttp = configuration["BasicKube:HttpBase"];
            ArgumentNullException.ThrowIfNull(baseHttp, "BasicKube:HttpBase");
            Client = httpClient;
            Client.BaseAddress = new Uri(baseHttp);
            _logger = logger;
        }

        private static string BuildUrlPrefix(
          int iamId,
          string actionName
      )
        {
            return $"{actionName}/{iamId}";
        }

        public async Task<bool> Del(
            int iamId,
            string podName
        )
        {
            try
            {
                string url = BuildUrlPrefix(iamId, "Del");

                using HttpResponseMessage response = await Client
                    .DeleteAsync($"{url}/{podName}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    var apiResult = JsonSerializer.Deserialize<ApiResultDto<bool?>>(jsonResponse);
                    return apiResult?.Code == 0;
                }
                else
                {
                    _logger.LogError("Pod Del failed, StatusCode: {0}", response.StatusCode);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Pod Del failed: {0}", e);
            }

            return false;
        }
    }
}