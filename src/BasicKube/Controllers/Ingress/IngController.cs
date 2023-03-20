using BasicKube.Api.Domain.Ing;

namespace BasicKube.Api.Controllers.App;

public class IngController 
    : KubeControllerBase, IGrpResControllerWithoutPublish<IngEditCommand>
{
    private readonly ILogger<IngController> _logger;

    private readonly IIngService _ingService;


    public IngController(
        IIngService ingService,
        ILogger<IngController> logger)
    {
        _ingService = ingService;
        _logger = logger;
    }

    /// <summary>
    /// 获取应用列表
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> ListGrp()
    {
        var ingList = await _ingService.ListGrpAsync(IamId);
        return ApiResult.BuildSuccess(ingList);
    }


    /// <summary>
    /// 获取应用列表
    /// </summary>
    /// <returns></returns>
    [HttpGet("{grpName}")]
    public async Task<IActionResult> List([FromRoute] string grpName)
    {
        IEnumerable<IngDetails> ingList;
        if (string.IsNullOrWhiteSpace(grpName))
        {
            ingList = await _ingService.ListInEnvAsync(IamId, EnvName!);
        }
        else
        {
            ingList = await _ingService.ListAsync(IamId, grpName);
        }
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
    public async Task<IActionResult> Create([FromBody] IngEditCommand command)
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


    [HttpDelete("{ingName}")]
    public async Task<IActionResult> Del([FromRoute] string ingName)
    {
        await _ingService.DelAsync(IamId, ingName);
        return ApiResult.Success;
    }

    #endregion edit


}