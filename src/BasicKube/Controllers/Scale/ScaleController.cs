using BasicKube.Api.Common.Components.ActionResultExtensions;
using BasicKube.Api.Controllers.Core;

namespace BasicKube.Api.Controllers.Scale;

public class ScalerController : KubeControllerBase
{
    private readonly KubernetesFactory _k8sFactory;
    private readonly ILogger<ScalerController> _logger;

    public ScalerController(
        ILogger<ScalerController> logger,
        KubernetesFactory k8sFactory
        )
    {
        _logger = logger;
        _k8sFactory = k8sFactory;
    }

    /// <summary>
    /// deployUnit 伸缩
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult> Scale([FromBody] ScaleDeployCommand command)
    {
        var patchStr = $@"
                    {{
                        ""spec"": {{
                            ""replicas"": {command.Replicas}
                        }}
                    }}";
        await _k8sFactory.MustGetByAppName(command.DeployName)
            .AppsV1
            .PatchNamespacedDeploymentScaleAsync(
            new V1Patch(patchStr, V1Patch.PatchType.MergePatch), command.DeployName, NsName
            );

        return ApiResult.Success;
    }


    #region AutoScale Deploy

    /// <summary>
    /// more see: https://juejin.cn/post/7086438714867449864
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult> CreateAutoScaleDeploy([FromBody] AutoScaleDeployCommand command)
    {
        var vha = CreateHpaBody(IamId, command, NsName);
        await _k8sFactory.MustGetByAppName(command.DeployName)
            .AutoscalingV2
            .CreateNamespacedHorizontalPodAutoscalerAsync(vha, NsName);
        return ApiResult.Success;
    }

    [HttpPut]
    public async Task<ActionResult> EditScaleDeploy([FromBody] AutoScaleDeployCommand command)
    {
        V2HorizontalPodAutoscaler vha = CreateHpaBody(IamId, command, NsName);
        await _k8sFactory.MustGetByAppName(command.DeployName)
            .AutoscalingV2
            .ReplaceNamespacedHorizontalPodAutoscalerStatusAsync(vha, vha.Metadata.Name, NsName);
        return ApiResult.Success;
    }

    private static V2HorizontalPodAutoscaler CreateHpaBody(int iamId, AutoScaleDeployCommand command, string ns)
    {
        var deployName = command.DeployName;
        var vha = new V2HorizontalPodAutoscaler()
        {
            Metadata = new V1ObjectMeta()
            {
                Name = GetHpaName(deployName),
                NamespaceProperty = ns,
                Annotations = new Dictionary<string, string>
                {
                    [K8sLabelsConstants.LabelDeployName] = deployName
                },
                Labels = new Dictionary<string, string>
                {
                    [K8sLabelsConstants.LabelDeployName] = deployName,
                    [K8sLabelsConstants.LabelIamId] = iamId + "",
                    [K8sLabelsConstants.LabelEnv] = deployName.Split('-')[^1]
                }
            },
            Spec = new V2HorizontalPodAutoscalerSpec()
            {
                ScaleTargetRef = new V2CrossVersionObjectReference()
                {
                    ApiVersion = "apps/v1",
                    Kind = "Deployment",
                    Name = ns,
                },
                MinReplicas = command.MinReplicas,
                MaxReplicas = command.MaxReplicas,
                Metrics = new List<V2MetricSpec>()
            }
        };

        foreach (var metricsInfo in command.Metrics)
        {
            V2MetricSpec? v2MetricSpec = null;
            switch (metricsInfo.Type)
            {
                case "Resource":
                    v2MetricSpec = new V2MetricSpec();
                    v2MetricSpec.Resource = new V2ResourceMetricSource()
                    {
                        Name = metricsInfo.Resource.Name,
                        Target = new V2MetricTarget
                        {
                            Type = metricsInfo.Resource.TargetType,
                            AverageUtilization = metricsInfo.Resource.TargetAverageUtilization,
                        }
                    };
                    break;

                default:
                    continue;
            }

            vha.Spec.Metrics.Add(v2MetricSpec);
        }
        return vha;
    }

    [HttpDelete("{deployName}")]
    public async Task<ActionResult> AutoScaleDeploy([FromRoute] string deployName)
    {
        await _k8sFactory.MustGetByAppName(deployName)
            .AutoscalingV2
            .DeleteNamespacedHorizontalPodAutoscalerAsync(
            GetHpaName(deployName),
                NsName
            );
        return ApiResult.Success;
    }

    private static string GetHpaName(string deployName)
    {
        return $"{deployName}-hpa";
    }

    #endregion AutoScale Deploy
}
