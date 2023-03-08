using BasicKube.Api.Common;
using k8s;
using KubeClient;
using System.ComponentModel;

namespace BasicKube.Api.Controllers.App;

public class AppManagerController : KubeControllerBase
{
    private readonly ILogger<AppManagerController> _logger;

    private readonly IKubernetes _kubernetes;
    private readonly KubeApiClient _kubeClient;

    public AppManagerController(
        IKubernetes kubernetes,
        KubeApiClient kubeClient,
        ILogger<AppManagerController> logger)
    {
        _kubernetes = kubernetes;
        _kubeClient = kubeClient;
        _logger = logger;
    }

    /// <summary>
    /// 获取 Deploy 应用列表
    /// </summary>
    /// <param name="ns"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult> DeployGrpList()
    {
        var deploymentListV1 = await _kubeClient.DeploymentsV1()
            .List($"{Constants.LableIamId}={IamId}", kubeNamespace: NsName);

        var appNames = deploymentListV1.Items
            .Select(x =>
            {
                if (x.Metadata.Labels.ContainsKey(Constants.LableAppGrpName))
                {
                    return x.Metadata.Labels[Constants.LableAppGrpName];
                }

                return "";
            })
            .ToHashSet()
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => new AppInfo()
            {
                Name = x
            })
            .ToList();

        return ApiResult.BuildSuccess(appNames);
    }


    /// <summary>
    /// 获取 DaemonSet 应用列表
    /// </summary>
    /// <param name="ns"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult> DaemonSetGrpList()
    {
        var deploymentListV1 = await _kubernetes.AppsV1
            .ListNamespacedDaemonSetAsync(NsName, labelSelector: $"{Constants.LableIamId}={IamId}");

        var appNames = deploymentListV1.Items
            .Select(x =>
            {
                if (!x.Metadata.Labels.ContainsKey(Constants.LableAppGrpName))
                {
                    return null;
                }
                var info = new DaemonSetAppInfo()
                {
                    Name = x.Metadata.Labels[Constants.LableAppGrpName]
                };

                var containers = x.Spec.Template.Spec.Containers;
                var mainContainer = containers[0];
                if (mainContainer.Ports is { Count: > 0 })
                {
                    var ports = info.Ports;
                    foreach (var item in mainContainer.Ports)
                    {
                        if (item.HostPort != null)
                        {
                            ports.Add(item.HostPort.Value);
                        }
                    }
                }

                return info;
            })
            .Where(x => x != null)
            .GroupBy(x => x!.Name)
            .Select(grp =>
            {
                return new DaemonSetAppInfo()
                {
                    Name= grp.Key,
                    Ports = grp.SelectMany(x => x!.Ports).ToList(),
                };
            });

        return ApiResult.BuildSuccess(appNames);
    }
}