using System.Net.Http.Json;

namespace BasicKube.Web.Services
{
    public class EventsHttp
    {
        public HttpClient Client { get; private set; }
        private readonly ILogger<EventsHttp> _logger;

        public EventsHttp(HttpClient httpClient, ILogger<EventsHttp> logger)
        {
            httpClient.BaseAddress = new Uri("http://localhost:5125");
            Client = httpClient;
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