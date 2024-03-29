﻿using BasicKube.Api.Controllers.Core;
using BasicKube.Api.Domain.AppGroup;

namespace BasicKube.Api.Controllers.App.Deploy
{
    public partial class DeployController
        : KubeControllerBase, IGrpResController<DeployEditCommand>
    {
        private readonly ILogger<DeployController> _logger;
        private readonly IDeployAppService _domainSvc;

        public DeployController(
            ILogger<DeployController> logger,
            IDeployAppService deployAppService
            )
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

        [HttpGet("{grpName?}")]
        public async Task<IActionResult> List([FromRoute] string? grpName)
        {
            var res = await _domainSvc.ListAsync(IamId, grpName, EnvName);
            return ApiResult.BuildSuccess(res);
        }

        #region edit

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] DeployEditCommand command
            )
        {
            await _domainSvc.CreateAsync(IamId, command);
            return ApiResult.Success;
        }

        [HttpPost]
        public async Task<IActionResult> Update(
            [FromBody] DeployEditCommand command
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