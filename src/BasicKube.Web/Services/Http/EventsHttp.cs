using System.Net.Http.Json;

namespace BasicKube.Web.Services
{
    public class EventsHttp
    {
        public HttpClient Client { get; private set; }
        private readonly ILogger<EventsHttp> _logger;

        public EventsHttp(
            IConfiguration configuration,
            HttpClient httpClient, 
            ILogger<EventsHttp> logger)
        {
            var baseHttp = configuration["BasicKube:HttpBase"];
            ArgumentNullException.ThrowIfNull(baseHttp, "BasicKube:HttpBase");
            Client = httpClient;
            Client.BaseAddress = new Uri(baseHttp);

            _logger = logger;
        }

        public async Task<List<EventInfo>> GetEvents(int iamId, string involvedObjectName)
        {
            var url = $"/api/Events/GetEvents/{iamId}/{involvedObjectName}";
            try
            {
                var data = await Client
                    .GetFromJsonAsync<ApiResultDto<List<EventInfo>>>(url);
                return data?.Data ?? new List<EventInfo>();
            }
            catch (Exception e)
            {
                _logger.LogError("GetSvcNameList failed: {0}", e);
                return new List<EventInfo>();
            }
        }
    }
}