using BasicKube.Api.Common.Components.ActionResultExtensions;
using BasicKube.Api.Controllers.Core;
using BasicKube.Api.Domain.AppGroup;

namespace BasicKube.Api.Controllers.App.Job
{
    public partial class JobController
        : KubeControllerBase, IGrpResController<JobEditCommand>
    {
        private readonly ILogger<JobController> _logger;
        private readonly IJobAppService _domainSvc;

        public JobController(
            ILogger<JobController> logger,
            IJobAppService domainSvc)
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


        #region edit

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JobEditCommand command)
        {
            await _domainSvc.CreateAsync(IamId, command);
            return ApiResult.Success;
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] JobEditCommand command)
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