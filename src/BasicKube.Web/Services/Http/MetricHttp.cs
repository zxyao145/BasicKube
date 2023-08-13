using System.Net.Http.Json;
using System.Text.Json;

namespace BasicKube.Web.Services
{
    public class MetricHttp
    {
        protected HttpClient Client { get; private set; }
        private readonly ILogger<MetricHttp> _logger;

        public MetricHttp(
            IConfiguration configuration, 
            HttpClient httpClient, 
            ILogger<MetricHttp> logger)
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
            return $"/api/Metrics/{actionName}/{iamId}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iamId"></param>
        /// <param name="envName"></param>
        /// <param name="grpName"></param>
        /// <returns>
        /// key: pod name
        /// value: metric data
        /// </returns>
        public async Task<Dictionary<string, PodMetricsItem>> ListWithEnv(
            int iamId,
            string envName,
            string grpName
        )
        {
            try
            {
                string url = BuildUrlPrefix(iamId, nameof(ListWithEnv));
                var apiResult = await Client
                    .GetFromJsonAsync<ApiResultDto<Dictionary<string, PodMetricsItem>>>
                        ($"{url}/{grpName}?env={envName}");
                return apiResult?.Data ?? new Dictionary<string, PodMetricsItem>();
            }
            catch (Exception e)
            {
                _logger.LogError("MetricHttp ListWithEnv failed: {0}", e);
            }

            return new Dictionary<string, PodMetricsItem>();
        }
    }
}