using System.Text.Json;
using BasicKube.Api.Config;
using Microsoft.Extensions.Options;

namespace BasicKube.Api.Domain.Prom;

public class PromHttpClient
{
    private readonly HttpClient _httpClient;

    public PromHttpClient(
        HttpClient httpClient,
        IOptions<K8sOptions> options
        )
    {
        _httpClient = httpClient;
        var promConfig = options.Value.PromConfig;
        if (promConfig.BaseAddr != "in-cluster")
        {
            _httpClient.BaseAddress = new Uri(promConfig.BaseAddr);
        }
    }

    public async Task<PromQueryResultModel?> Query(string pathWithQuery)
    {
        var resp = await _httpClient.GetAsync(pathWithQuery);

        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }

        Stream utf8Json = await resp.Content.ReadAsStreamAsync();
        return await JsonSerializer
            .DeserializeAsync<PromQueryResultModel>(utf8Json);
    }
}
