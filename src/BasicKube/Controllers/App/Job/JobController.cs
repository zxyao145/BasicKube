using BasicKube.Api.Controllers.App;
using BasicKube.Api.Domain.AppGroup;

namespace BasicKube.Api.Controllers.Job
{
    public partial class JobController : KubeControllerBase
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
        public async Task<ActionResult> ListGrp()
        {
            var res = await _domainSvc.ListGrpAsync(IamId);
            return ApiResult.BuildSuccess(res);
        }

        [HttpGet("{grpName}")]
        public async Task<IActionResult> List([FromRoute] string grpName)
        {
            var services = await _domainSvc.ListAsync(IamId, grpName);
            return ApiResult.BuildSuccess(services);
        }


        [HttpGet("{resName}")]
        public async Task<IActionResult> Details([FromRoute] string resName)
        {
            var cmd = await _domainSvc.DetailsAsync(IamId, resName);
            if (cmd == null)
            {
                return NotFound($"Resource {resName} not found");
            }

            return ApiResult.BuildSuccess(cmd);
        }


        #region edit

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] JobEditCommand command)
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
        public async Task<ActionResult> Publish(
            [FromBody] AppPublishCommand command
        )
        {
            await _domainSvc.PublishAsync(IamId, command);
            return ApiResult.Success;
        }

        #endregion edit


        [HttpDelete("{grpName}/{resName}")]
        public async Task<IActionResult> Del([FromRoute] string resName)
        {
            await _domainSvc.DelAsync(IamId, resName);
            return ApiResult.Success;
        }
    }
}