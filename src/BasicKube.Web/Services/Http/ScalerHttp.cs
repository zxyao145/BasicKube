using System.Text;
using System.Text.Json;

namespace BasicKube.Web.Services
{
    public class ScalerHttp
    {
        public HttpClient Client { get; private set; }
        private readonly ILogger<ScalerHttp> _logger;

        public ScalerHttp(
            IConfiguration configuration,
            HttpClient httpClient, 
            ILogger<ScalerHttp> logger)
        {
            var baseHttp = configuration["BasicKube:HttpBase"];
            ArgumentNullException.ThrowIfNull(baseHttp, "BasicKube:HttpBase");
            Client = httpClient;
            Client.BaseAddress = new Uri(baseHttp);
            _logger = logger;
        }

        public async Task<bool> AppScale(
            int iamId,
            string resName,
            int replicas
        )
        {
            var scaleCmd = new ScaleDeployCommand
            {
                DeployName = resName,
                Replicas = replicas,
                IamId = replicas,
            };
            var json = JsonSerializer.Serialize(scaleCmd);

            var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                using HttpResponseMessage response = await Client
                    .PostAsync($"Scale/{iamId}", jsonContent);

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
    }
}