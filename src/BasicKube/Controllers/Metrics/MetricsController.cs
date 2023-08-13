using System.Text.Encodings.Web;
using System.Text.Json;
using BasicKube.Api.Controllers.Core;
using BasicKube.Api.Domain;
using BasicKube.Api.Domain.Metrics;

namespace BasicKube.Api.Controllers.Metrics;

public class MetricsController : KubeControllerBase
{
    private readonly ILogger<MetricsController> _logger;
    private readonly IMetricsService _metricsService;

    public MetricsController(
        ILogger<MetricsController> logger,
        IMetricsService metricsService)
    {
        _logger = logger;
        _metricsService = metricsService;
    }

    /// <summary>
    /// 使用 Metrics Server
    /// </summary>
    /// <param name="grpName"></param>
    /// <returns></returns>
    [HttpGet("{grpName}")]
    public async Task<IActionResult> ListWithEnv([FromRoute] string grpName)
    {
        var details = await _metricsService
            .GetPodMetricsList(IamId, grpName, EnvName!);
        return ApiResult.BuildSuccess(details);
    }

    /// <summary>
    /// 使用 Prometheus 指标
    /// </summary>
    /// <param name="env"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> ListPromPods(
        [FromQuery] string env,
        [FromBody] PodPromMetricQuery query
        )
    {
        var details = await _metricsService
            .GetPromPodMetricsList(
                IamId,
                query,
                EnvName!
                );
        return ApiResult.BuildSuccess(details);
    }
}
