using BasicKube.Api.Controllers.Deploy;
using KubeClient;

namespace BasicKube.Api.Controllers.AutoScale;

public class ScalerController : KubeControllerBase
{
    private readonly IKubernetes _kubernetes;
    private readonly ILogger<ScalerController> _logger;

    public ScalerController(
        ILogger<ScalerController> logger,
        IKubernetes kubernetes
        )
    {
        _logger = logger;
        _kubernetes = kubernetes;
    }

    /// <summary>
    /// deployUnit 伸缩
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult> Scale([FromBody] DeployScaleCommand command)
    {
        var patchStr = $@"
                    {{
                        ""spec"": {{
                            ""replicas"": {command.Replicas}
                        }}
                    }}";
        await _kubernetes.AppsV1.PatchNamespacedDeploymentScaleAsync(
            new V1Patch(patchStr, V1Patch.PatchType.MergePatch), command.DeployName, NsName
            );

        // _kubeClient.DeploymentsV1().Update(command.DeployName, doc =>
        // {
        //    doc.Replace(x => x.Spec.Replicas, command.Replicas < 0 ? 0 : command.Replicas);
        // });
        return ApiResult.Success;
    }

    #region AutoScale

    /// <summary>
    /// more see: https://juejin.cn/post/7086438714867449864
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult> CreateAutoScale([FromBody] AutoScaleCreateCommand command)
    {
        var vha = CreateHpaBody(IamId, command, NsName);
        await _kubernetes.AutoscalingV2.CreateNamespacedHorizontalPodAutoscalerAsync(vha, NsName);

        return ApiResult.Success;
    }

    [HttpPost]
    public async Task<ActionResult> EditAutoScale([FromBody] AutoScaleCreateCommand command)
    {
        V2HorizontalPodAutoscaler vha = CreateHpaBody(IamId, command, NsName);
        await _kubernetes.AutoscalingV2
            .ReplaceNamespacedHorizontalPodAutoscalerStatusAsync(vha, vha.Metadata.Name, NsName);

        return ApiResult.Success;
    }

    private static V2HorizontalPodAutoscaler CreateHpaBody(int iamId, AutoScaleCreateCommand command, string ns)
    {
        var deployName = command.DeployName;
        var vha = new V2HorizontalPodAutoscaler()
        {
            Metadata = new V1ObjectMeta()
            {
                Name = $"{deployName}-hpa",
                NamespaceProperty = ns,
                Annotations = new Dictionary<string, string>
                {
                    ["deployName"] = deployName
                },
                Labels = new Dictionary<string, string>
                {
                    ["deployName"] = deployName,
                    ["iamId"] = iamId + "",
                    ["env"] = deployName.Split('-')[^1]
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
    public async Task<ActionResult> DelAutoScale([FromRoute] string deployName)
    {
        var name = $"{deployName}-hpa";
        await _kubernetes.AutoscalingV2.DeleteNamespacedHorizontalPodAutoscalerAsync(name, NsName);
        return ApiResult.Success;
    }

    #endregion
}
