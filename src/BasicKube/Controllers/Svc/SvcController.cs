using BasicKube.Api.Common;
using BasicKube.Api.Domain.Svc;
using KubeClient.Models;

namespace BasicKube.Api.Controllers.Svc;

public class SvcController 
    : KubeControllerBase, IGrpResControllerWithoutPublish<SvcEditCommand>
{
    private readonly ILogger<SvcController> _logger;
    private readonly ISvcService _svcService;


    public SvcController(
        ILogger<SvcController> logger,
        ISvcService svcService)
    {
        _logger = logger;
        _svcService = svcService;
    }

    [HttpGet]
    public async Task<IActionResult> ListGrp()
    {
        var grp = await _svcService.ListGrpAsync(IamId);
        return ApiResult.BuildSuccess(grp);
    }


    [HttpGet("{svcName?}")]
    public async Task<IActionResult> List([FromRoute] string? svcName)
    {
        var services = await _svcService.ListAsync(IamId, svcName);
        return ApiResult.BuildSuccess(services);
    }


    [HttpGet("{svcName}")]
    public async Task<IActionResult> Details([FromRoute] string svcName)
    {
        var cmd = await _svcService.DetailsAsync(IamId, svcName);
        if (cmd == null)
        {
            return NotFound($"service {svcName} not found");
        }

        return ApiResult.BuildSuccess(cmd);
    }


    #region edit

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SvcEditCommand command)
    {
        await _svcService.CreateAsync(IamId, command);
        return ApiResult.Success;
    }

    [HttpPost]
    public async Task<IActionResult> Update([FromBody] SvcEditCommand command)
    {
        await _svcService.UpdateAsync(IamId, command);
        return ApiResult.Success;
    }

    #endregion edit


    [HttpDelete("{svcName}")]
    public async Task<IActionResult> Del([FromRoute] string svcName)
    {
        await _svcService.DelAsync(IamId, svcName);
        return ApiResult.Success;
    }
}