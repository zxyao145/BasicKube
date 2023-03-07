using KubeClient;

namespace BasicKube.Api.Controllers.App;

public class GatewayController : KubeControllerBase
{
    private readonly ILogger<GatewayController> _logger;

    private readonly IKubernetes _kubernetes;

    public GatewayController(
        IKubernetes kubernetes,
        ILogger<GatewayController> logger)
    {
        _kubernetes = kubernetes;
        _logger = logger;
    }

    /// <summary>
    /// 获取应用列表
    /// </summary>
    
    /// <param name="ns"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult> GetAppList()
    {
        //var ingList = await _kubernetes.NetworkingV1
        //    .ListNamespacedIngressAsync(ns);
        await Task.CompletedTask;
        return ApiResult.BuildSuccess("");
    }
}