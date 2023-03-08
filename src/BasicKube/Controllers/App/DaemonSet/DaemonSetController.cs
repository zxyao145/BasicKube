using BasicKube.Api.Controllers.App;
using BasicKube.Api.Domain.App;
using BasicKube.Api.Domain.Pod;

namespace BasicKube.Api.Controllers.App.DaemonSet
{
    public partial class DaemonSetController : AppControllerBase
    {

        private readonly ILogger<DaemonSetController> _logger;
        private readonly DaemonSetAppService _daemonSetAppService;

        public DaemonSetController(
            ILogger<DaemonSetController> logger,
            IPodService podService,
            DaemonSetAppService deployAppService)
        {
            _logger = logger;
            _daemonSetAppService = deployAppService;
        }

        /// <summary>
        /// 查询 appName 中的 deploy 及其 Pod 列表
        /// </summary>

        /// <param name="appName"></param>
        /// <param name="env"></param>
        /// <param name="ns"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> List(
            [FromQuery] string? env
            )
        {
            var res = await _daemonSetAppService.ListAsync(IamId, AppName, env);
            return ApiResult.BuildSuccess(res);
        }

        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] DaemonSetCreateCommand command
            )
        {
            await _daemonSetAppService.CreateAsync(IamId, command);
            return ApiResult.Success;
        }

        [HttpPost]
        public async Task<ActionResult> Update(
            [FromBody] DaemonSetCreateCommand command
            )
        {
            await _daemonSetAppService.UpdateAsync(IamId, command);
            return ApiResult.Success;
        }

        [HttpPut]
        public async Task<ActionResult> Publish(
            [FromBody] AppPublishCommand command
        )
        {
            await _daemonSetAppService.PublishAsync(IamId, command);
            return ApiResult.Success;
        }

        [HttpDelete("{deployUnitName}")]
        public async Task<ActionResult> Del(
            [FromRoute] string deployUnitName
        )
        {
            await _daemonSetAppService.DelAsync(IamId, deployUnitName);
            return ApiResult.Success;
        }


        [HttpGet("{deployUnitName}")]
        public async Task<ActionResult> Details(
            [FromRoute] string deployUnitName
            )
        {
            var cmd = await _daemonSetAppService.DetailsAsync(IamId, deployUnitName);
            return ApiResult.BuildSuccess(cmd);
        }
    }
}