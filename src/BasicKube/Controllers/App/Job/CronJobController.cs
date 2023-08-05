using BasicKube.Api.Common.Components.ActionResultExtensions;
using BasicKube.Api.Controllers.Core;
using BasicKube.Api.Domain.AppGroup;

namespace BasicKube.Api.Controllers.App.Job
{
    public partial class CronJobController
        : KubeControllerBase, IGrpResController<CronJobEditCommand>
    {
        private readonly ILogger<CronJobController> _logger;
        private readonly ICronJobAppService _domainSvc;

        public CronJobController(
            ILogger<CronJobController> logger,
            ICronJobAppService domainSvc)
        {
            _logger = logger;
            _domainSvc = domainSvc;
        }

        [HttpGet]
        public async Task<IActionResult> ListGrp()
        {
            var res = await _domainSvc.ListGrpAsync(IamId);
            return ApiResult.BuildSuccess(res);
        }

        [HttpGet("{grpName}")]
        public async Task<IActionResult> List(
            [FromRoute] string grpName
            )
        {
            var services = await _domainSvc.ListAsync(IamId, grpName);
            return ApiResult.BuildSuccess(services);
        }

        [HttpGet("{appName}")]
        public async Task<IActionResult> Details([FromRoute] string appName)
        {
            var cmd = await _domainSvc.DetailsAsync(IamId, appName);
            if (cmd == null)
            {
                return NotFound($"Resource {appName} not found");
            }

            return ApiResult.BuildSuccess(cmd);
        }

        [HttpPut]
        public async Task<IActionResult> Suspend([FromBody] CronJobSuspendCommand command)
        {
            await _domainSvc.Suspend(IamId, command);
            return ApiResult.Success;
        }


        #region edit

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CronJobEditCommand command)
        {
            await _domainSvc.CreateAsync(IamId, command);
            return ApiResult.Success;
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] CronJobEditCommand command)
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
    }
}