using BasicKube.Api.Controllers.App;
using BasicKube.Api.Domain.App;
using BasicKube.Api.Domain.Pod;
using Json.Patch;
using k8s;
using KubeClient;
using KubeClient.Models;
using Org.BouncyCastle.Security;
using System.Text.Json;

namespace BasicKube.Api.Controllers.Deploy
{
    public partial class DeployController : AppControllerBase
    {
        private readonly IKubernetes _kubernetes;
        private readonly ILogger<DeployController> _logger;
        private readonly DeployAppService _deployAppService;

        public DeployController(
            ILogger<DeployController> logger,
            DeployAppService deployAppService,
            IKubernetes kubernetes)
        {
            _logger = logger;
            _deployAppService = deployAppService;
            _kubernetes = kubernetes;
        }

        /// <summary>
        /// 查询 appName 中的 deploy 及其 Pod 列表
        /// </summary>

        /// <param name="appName"></param>
        /// <param name="env"></param>
        /// <param name="ns"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> GetDeployUnitList(
            [FromQuery] string? env
            )
        {
            var res = await _deployAppService.ListAsync(IamId, AppName, env);
            return ApiResult.BuildSuccess(res);
        }

        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] DeployCreateCommand command
            )
        {
            await _deployAppService.CreateAsync(IamId, command);
            return ApiResult.Success;
        }

        [HttpPost]
        public async Task<ActionResult> Update(
            [FromBody] DeployCreateCommand command
            )
        {
            await _deployAppService.UpdateAsync(IamId, command);
            return ApiResult.Success;
        }

        [HttpPut]
        public async Task<ActionResult> Publish(
            [FromBody] AppPublishCommand command
        )
        {
            await _deployAppService.PublishAsync(IamId, command);
            return ApiResult.Success;
        }

        [HttpDelete("{deployUnitName}")]
        public async Task<ActionResult> Del(
            [FromRoute] string deployUnitName
        )
        {
            await _deployAppService.DelAsync(IamId, deployUnitName);
            return ApiResult.Success;
        }


        [HttpGet("{deployUnitName}")]
        public async Task<ActionResult> Details(
            [FromRoute] string deployUnitName
            )
        {
            var cmd = await _deployAppService.DetailsAsync(IamId, deployUnitName);
            return ApiResult.BuildSuccess(cmd);
        }
    }
}