using BasicKube.Api.Common;
using BasicKube.Api.Domain.Pod;
using BasicKube.Api.Exceptions;
using Json.Patch;
using System.Text.Json;

namespace BasicKube.Api.Domain.App;

public interface IDaemonSetAppService 
    : IAppService<DaemonSetGrpInfo, DaemonSetDetails, DaemonSetEditCommand>
{

}


[Service<IDaemonSetAppService>]
public class DaemonSetAppService 
    : AppServiceBase<DaemonSetGrpInfo, DaemonSetDetails, DaemonSetEditCommand>
    , IDaemonSetAppService
{
    private readonly IKubernetes _kubernetes;

    private readonly ILogger<DaemonSetAppService> _logger;

    public DaemonSetAppService(IamService iamService, IKubernetes kubernetes, ILogger<DaemonSetAppService> logger) : base(iamService)
    {
        _kubernetes = kubernetes;
        _logger = logger;
    }

    public override async Task<IEnumerable<DaemonSetGrpInfo>> ListGrpAsync
        (int iamId)
    {
        var nsName = IamService.GetNsName(iamId);

        var deploymentListV1 = await _kubernetes.AppsV1
            .ListNamespacedDaemonSetAsync(
                nsName, 
                labelSelector: $"{K8sLabelsConstants.LabelIamId}={iamId}"
            );

        var appNames = deploymentListV1.Items
            .Select(x =>
            {
                if (!x.Metadata.Labels.ContainsKey(K8sLabelsConstants.LabelAppGrpName))
                {
                    return null;
                }
                var info = new DaemonSetGrpInfo()
                {
                    Name = x.Metadata.Labels[K8sLabelsConstants.LabelAppGrpName]
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
                return new DaemonSetGrpInfo()
                {
                    Name = grp.Key,
                    Ports = grp.SelectMany(x => x!.Ports).ToList(),
                };
            });

        return appNames;
    }


    #region edit

    public override async Task CreateAsync(int iamId, DaemonSetEditCommand command)
    {
        var nsName = IamService.GetNsName(iamId);
        var v1DaemonSet = CreateKubeApp<V1DaemonSet>(nsName, command);
        await _kubernetes.AppsV1
            .CreateNamespacedDaemonSetAsync(v1DaemonSet, v1DaemonSet.Metadata.NamespaceProperty);
    }


    public override async Task UpdateAsync(int iamId, DaemonSetEditCommand command)
    {
        var nsName = IamService.GetNsName(iamId);
        var v1DaemonSet = CreateKubeApp<V1DaemonSet>(nsName, command);
        await _kubernetes.AppsV1
            .ReplaceNamespacedDaemonSetAsync(v1DaemonSet, v1DaemonSet.Metadata.Name,
            v1DaemonSet.Metadata.NamespaceProperty
            );
    }

    #endregion

    public override async Task DelAsync(int iamId, string resName)
    {
        var nsName = IamService.GetNsName(iamId);
        await _kubernetes.AppsV1.DeleteNamespacedDaemonSetAsync(resName, nsName);
    }

    #region Details

    public override async Task<DaemonSetEditCommand?> DetailsAsync(int iamId, string resName)
    {
        var nsName = IamService.GetNsName(iamId);
        var daemonSetment = await _kubernetes.AppsV1
                .ReadNamespacedDaemonSetAsync(resName, nsName);
        if (daemonSetment == null)
        {
            return null;
        }

        var cmd = GetAppCreateCommand<DaemonSetEditCommand>(resName, daemonSetment);

        return cmd;
    }

    #endregion


    public override async Task<IEnumerable<DaemonSetDetails>> ListAsync
        (int iamId, string? appName, string? env = null)
    {
        var nsName = IamService.GetNsName(iamId);
        var labelSelector = $"{K8sLabelsConstants.LabelIamId}={iamId}," +
            $"{K8sLabelsConstants.LabelAppGrpName}={appName}," +
            $"{K8sLabelsConstants.LabelAppType}={DaemonSetEditCommand.Type}";
        if (!string.IsNullOrWhiteSpace(env))
        {
            labelSelector += $",{K8sLabelsConstants.LabelEnv}={env}";
        }

        var deploys = await _kubernetes.AppsV1
            .ListNamespacedDaemonSetAsync(
                nsName,
                labelSelector: labelSelector
                );

        if (deploys.Items.Count < 1)
        {
            return new List<DaemonSetDetails>();
        }

        var deploysList = deploys.Items;
        var result = new List<DaemonSetDetails>();
        var allPods = (
                await _kubernetes.CoreV1
                    .ListNamespacedPodAsync(nsName, 
                    labelSelector: $"{K8sLabelsConstants.LabelIamId}={iamId},{K8sLabelsConstants.LabelAppType}={DaemonSetEditCommand.Type}")
            ).Items
            .OrderBy(x => x.Status.StartTime)
            .ThenBy(x => x.Metadata.Name)
            .ToList();

        foreach (var deploy in deploysList)
        {
            var curDeployName = deploy.Metadata.Name;
            var details = new DaemonSetDetails
            {
                Name = curDeployName,
            };
            var count = allPods.Count;

            for (int i = count - 1; i > -1; i--)
            {
                var curPod = allPods[i];
                var podName = curPod.Metadata.Name;
                if (podName.StartsWith(curDeployName))
                {
                    allPods.RemoveAt(i);
                    var podDetail = PodService.GetPodDetail(curPod);
                    details.PodDetails.Add(podDetail);
                }
            }

            details.PodDetails = details.PodDetails
                .OrderBy(x => x.Name)
                .ToList();
            result.Add(details);
        }

        return result.OrderBy(x => x.Name);
    }


    public override async Task PublishAsync(
        int iamId, 
        AppPublishCommand command
        )
    {
        string daemonSetName = command.AppName;
        var nsName = IamService.GetNsName(iamId);
        var daemonSet = await _kubernetes
                .AppsV1
                .ReadNamespacedDaemonSetAsync(daemonSetName, nsName);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
        var old = JsonSerializer.SerializeToDocument(daemonSet, options);


        var imgName = daemonSet.Spec.Template.Spec.Containers[0].Image.Split(":")[0];
        var newImg = $"{imgName}:{command.Tag}";
        daemonSet.Spec.Template.Spec.Containers[0].Image = newImg;
        daemonSet.Metadata.Annotations.Add("kubernetes.io/change-cause", command.Description ?? "");

        var expected = JsonSerializer.SerializeToDocument(daemonSet);
        // JsonPatch.Net
        var patch = old.CreatePatch(expected);
        var patchResponse = await _kubernetes
            .AppsV1
            .PatchNamespacedDaemonSetAsync(
            new V1Patch(patch, V1Patch.PatchType.JsonPatch),
            daemonSetName,
            nsName
            );
    }
}
