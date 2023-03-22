using BasicKube.Api.Domain.App;

namespace BasicKube.Api.Controllers.App.DaemonSet
{
    public partial class DaemonSetController
        : KubeControllerBase, IGrpResController<DaemonSetEditCommand>
    {
        private readonly ILogger<DaemonSetController> _logger;
        private readonly IDaemonSetAppService _domainSvc;

        public DaemonSetController(
            ILogger<DaemonSetController> logger,
            IDaemonSetAppService deployAppService)
        {
            _logger = logger;
            _domainSvc = deployAppService;
        }

        [HttpGet]
        public async Task<IActionResult> ListGrp()
        {
            var res = await _domainSvc.ListGrpAsync(IamId);
            return ApiResult.BuildSuccess(res);
        }


        [HttpGet("{grpName}")]
        public async Task<IActionResult> List
            (
                [FromRoute] string grpName
            )
        {
            var res = await _domainSvc.ListAsync(IamId, grpName);
            return ApiResult.BuildSuccess(res);
        }


        #region edit

        [HttpPost]
        public async Task<IActionResult> Create(
                [FromBody] DaemonSetEditCommand command
            )
        {
            await _domainSvc.CreateAsync(IamId, command);
            return ApiResult.Success;
        }

        [HttpPost]
        public async Task<IActionResult> Update(
                [FromBody] DaemonSetEditCommand command
            )
        {
            await _domainSvc.UpdateAsync(IamId, command);
            return ApiResult.Success;
        }

        [HttpPut]
        public async Task<IActionResult> Publish(
            [FromBody] AppPublishCommand command
        )
        {
            await _domainSvc.PublishAsync(IamId, command);
            return ApiResult.Success;
        }

        #endregion edit


        [HttpDelete("{appName}")]
        public async Task<IActionResult> Del(
            [FromRoute] string appName
        )
        {
            await _domainSvc.DelAsync(IamId, appName);
            return ApiResult.Success;
        }


        [HttpGet("{appName}")]
        public async Task<IActionResult> Details(
            [FromRoute] string appName
            )
        {
            var cmd = await _domainSvc.DetailsAsync(IamId, appName);
            return ApiResult.BuildSuccess(cmd);
        }
    }
}