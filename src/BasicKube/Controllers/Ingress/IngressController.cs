using BasicKube.Api.Domain.Ing;
using KubeClient;

namespace BasicKube.Api.Controllers.App;

public class IngressController : KubeControllerBase
{
    private readonly ILogger<IngressController> _logger;

    private readonly IIngService _ingService;



    public IngressController(
        IIngService ingService,
        ILogger<IngressController> logger)
    {
        _ingService = ingService;
        _logger = logger;
    }

    /// <summary>
    /// 获取应用列表
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult> ListGrp()
    {
        var ingList = await _ingService.ListGrpAsync(IamId);
        return ApiResult.BuildSuccess(ingList);
    }


    /// <summary>
    /// 获取应用列表
    /// </summary>
    /// <returns></returns>
    [HttpGet("{grpName}")]
    public async Task<ActionResult> List([FromRoute] string grpName)
    {
        var ingList = await _ingService.ListAsync(IamId, grpName);
        return ApiResult.BuildSuccess(ingList);
    }

    [HttpGet("{ingName}")]
    public async Task<IActionResult> Details([FromRoute] string ingName)
    {
        var cmd = await _ingService.DetailsAsync(IamId, ingName);
        if (cmd == null)
        {
            return NotFound($"ingress {ingName} not found");
        }

        return ApiResult.BuildSuccess(cmd);
    }

    #region edit

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] IngEditCommand command)
    {
        await _ingService.CreateAsync(IamId, command);
        return ApiResult.Success;
    }

    [HttpPost]
    public async Task<IActionResult> Update([FromBody] IngEditCommand command)
    {
        await _ingService.UpdateAsync(IamId, command);
        return ApiResult.Success;
    }

    #endregion edit


    [HttpDelete("{ingName}")]
    public async Task<IActionResult> Del([FromRoute] string ingName)
    {
        await _ingService.DelAsync(IamId, ingName);
        return ApiResult.Success;
    }
}