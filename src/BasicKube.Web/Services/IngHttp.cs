using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace BasicKube.Web.Services
{
    public class IngHttp
    {
        public HttpClient Client { get; private set; }
        private readonly ILogger<IngHttp> _logger;

        public IngHttp(HttpClient httpClient, ILogger<IngHttp> logger)
        {
            httpClient.BaseAddress = new Uri("http://localhost:5125/api/Ingress/");
            Client = httpClient;
            _logger = logger;
        }

        public async Task<List<IngGrpInfo>> ListGrp(int iamId)
        {
            var url = $"ListGrp/{iamId}";
            try
            {
                var data = await Client
                    .GetFromJsonAsync<ApiResultDto<List<IngGrpInfo>>>(url);
                return data?.Data ?? new List<IngGrpInfo>();
            }
            catch (Exception e)
            {
                _logger.LogError("ListGrp failed: {0}", e);
                return new List<IngGrpInfo>();
            }
        }

        public async Task<List<IngDetails>> List(
            int iamId,
            string grpName)
        {
            var url = $"List/{iamId}/{grpName}";
            try
            {
                var data = await Client
                    .GetFromJsonAsync<ApiResultDto<List<IngDetails>>>(url);
                return data?.Data ?? new List<IngDetails>();
            }
            catch (Exception e)
            {
                _logger.LogError("List failed: {0}", e);
                return new List<IngDetails>();
            }
        }

        public async Task<bool> Add(IngEditCommand command)
        {
            var url = $"Add/{command.IamId}";
            try
            {
                var json = JsonSerializer.Serialize(command);
                var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await Client.PostAsync(url, jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var apiResult = JsonSerializer.Deserialize<ApiResultDto<bool?>>(jsonResponse);
                    return apiResult?.Code == 0;
                }
                else
                {
                    _logger.LogError("Add failed, StatusCode: {0}", response.StatusCode);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Add failed: {0}", e);
            }
            return false;
        }

        public async Task<bool> Update(IngEditCommand command)
        {
            var url = $"Update/{command.IamId}";
            try
            {
                var json = JsonSerializer.Serialize(command);
                var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await Client.PostAsync(url, jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var apiResult = JsonSerializer.Deserialize<ApiResultDto<bool?>>(jsonResponse);
                    return apiResult?.Code == 0;
                }
                else
                {
                    _logger.LogError("Update failed, StatusCode: {0}", response.StatusCode);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Update failed: {0}", e);
            }
            return false;
        }

        public async Task<bool> Del(int iamId, string ingName)
        {
            var url = $"Del/{iamId}/{ingName}";
            try
            {
                var response = await Client
                    .DeleteAsync(url);
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

        public async Task<IngEditCommand?> Details(int iamId, string ingName)
        {
            var url = $"Details/{iamId}/{ingName}";
            try
            {
                var data = await Client.GetFromJsonAsync<ApiResultDto<IngEditCommand>>
                    ($"{url}");
                return data?.Data;
            }
            catch (Exception e)
            {
                _logger.LogError("Details failed: {0}", e);
                return null;
            }
        }
    }
}