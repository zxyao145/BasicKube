using BasicKube.Api.Domain.Pod;

namespace BasicKube.Api.Controllers.Pod;

public class PodController : KubeControllerBase
{
    private readonly IPodService _podService;
    private readonly ILogger<PodController> _logger;

    public PodController(
        ILogger<PodController> logger,
        IPodService podService)
    {
        _podService = podService;
        _logger = logger;
    }


    /// <summary>
    /// 删除Pod，如果pod 属于某个app，则会重建
    /// </summary>
    /// <param name="podName"></param>
    /// <param name="ns"></param>
    /// <returns></returns>
    [HttpDelete("{podName}")]
    public async Task<ActionResult> Del(
        [FromRoute] string podName
    )
    {
        await _podService.DelAsync(podName, NsName);
        return ApiResult.Success;
    }
}
