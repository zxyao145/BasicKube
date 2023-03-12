using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace BasicKube.Web.Services
{
    public class JobHttp
    {
        public HttpClient Client { get; private set; }
        private readonly ILogger<JobHttp> _logger;

        public JobHttp(HttpClient httpClient, ILogger<JobHttp> logger)
        {
            httpClient.BaseAddress = new Uri("http://localhost:5125/api/Job/");
            Client = httpClient;
            _logger = logger;
        }

        public async Task<List<JobGrpInfo>> ListGrp(int iamId)
        {
            var url = $"ListGrp/{iamId}";
            try
            {
                var data = await Client
                    .GetFromJsonAsync<ApiResultDto<List<JobGrpInfo>>>(url);
                return data?.Data ?? new List<JobGrpInfo>();
            }
            catch (Exception e)
            {
                _logger.LogError("ListGrp failed: {0}", e);
                return new List<JobGrpInfo>();
            }
        }

        public async Task<List<JobDetails>> List(
           int iamId,
           string grpName)
        {
            var url = $"List/{iamId}/{grpName}";
            try
            {
                var data = await Client
                    .GetFromJsonAsync<ApiResultDto<List<JobDetails>>>(url);
                return data?.Data ?? new List<JobDetails>();
            }
            catch (Exception e)
            {
                _logger.LogError("List failed: {0}", e);
                return new List<JobDetails>();
            }
        }


        public async Task<bool> Add(JobEditCommand command)
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

        public async Task<bool> Update(JobEditCommand command)
        {
            var url = $"Update/{command.IamId}";
            try
            {
                var json = JsonSerializer.Serialize(command);
                Console.WriteLine(json);
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

        public async Task<bool> Del(int iamId, string svcName)
        {
            var url = $"Del/{iamId}/{svcName}";
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

        public async Task<JobEditCommand?> Details(int iamId, string svcName)
        {
            var url = $"Details/{iamId}/{svcName}";
            try
            {
                var data = await Client.GetFromJsonAsync<ApiResultDto<JobEditCommand>>
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