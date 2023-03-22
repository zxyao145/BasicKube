using BasicKube.Api.Domain.Pod;
using Json.Patch;
using System.Text.Json;

namespace BasicKube.Api.Domain.AppGroup;

public interface IDaemonSetAppService
    : IAppService<DaemonSetGrpInfo, DaemonSetDetails, DaemonSetEditCommand>
{
}


[Service<IDaemonSetAppService>]
public class DaemonSetAppService
    : AppServiceBase<DaemonSetGrpInfo, DaemonSetDetails, DaemonSetEditCommand>
    , IDaemonSetAppService
{
    private readonly KubernetesFactory _k8sFactory;

    private readonly ILogger<DaemonSetAppService> _logger;

    public DaemonSetAppService(IamService iamService, KubernetesFactory kubernetes, ILogger<DaemonSetAppService> logger) : base(iamService)
    {
        _k8sFactory = kubernetes;
        _logger = logger;
    }

    public override async Task<IEnumerable<DaemonSetGrpInfo>> ListGrpAsync
        (int iamId)
    {
        var nsName = IamService.GetNsName(iamId);
        List<V1DaemonSet> allRes = new();
        var labelSelector = $"{K8sLabelsConstants.LabelIamId}={iamId}";
        foreach (var item in _k8sFactory.All)
        {
            var cutLabel = $"{labelSelector},{K8sLabelsConstants.LabelEnv}={item.Key}";
            var kubernetes = item.Value;
            var deploymentListV1 = await kubernetes.AppsV1
                .ListNamespacedDaemonSetAsync(
                    nsName,
                    labelSelector: cutLabel
                );
            if (deploymentListV1 != null)
            {
                allRes.AddRange(deploymentListV1.Items);
            }
        }

        var appNames = allRes
            .Select(x =>
            {
                if (!x.Metadata.Labels.ContainsKey(K8sLabelsConstants.LabelGrpName))
                {
                    return null;
                }
                var info = new DaemonSetGrpInfo()
                {
                    Name = x.Metadata.Labels[K8sLabelsConstants.LabelGrpName]
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
        var kubernetes = _k8sFactory.MustGet(command.Env);
        var nsName = IamService.GetNsName(iamId);
        var v1DaemonSet = CreateKubeApp<V1DaemonSet>(nsName, command);
        await kubernetes.AppsV1
            .CreateNamespacedDaemonSetAsync(v1DaemonSet, v1DaemonSet.Metadata.NamespaceProperty);
    }


    public override async Task UpdateAsync(int iamId, DaemonSetEditCommand command)
    {
        var kubernetes = _k8sFactory.MustGet(command.Env);
        var nsName = IamService.GetNsName(iamId);
        var v1DaemonSet = CreateKubeApp<V1DaemonSet>(nsName, command);
        await kubernetes.AppsV1
            .ReplaceNamespacedDaemonSetAsync(v1DaemonSet, v1DaemonSet.Metadata.Name,
            v1DaemonSet.Metadata.NamespaceProperty
            );
    }

    #endregion edit

    public override async Task DelAsync(int iamId, string resName)
    {
        var kubernetes = _k8sFactory.MustGetByAppName(resName);
        var nsName = IamService.GetNsName(iamId);
        await kubernetes.AppsV1.DeleteNamespacedDaemonSetAsync(resName, nsName);
    }

    #region Details

    public override async Task<DaemonSetEditCommand?> DetailsAsync(int iamId, string resName)
    {
        var kubernetes = _k8sFactory.MustGetByAppName(resName);
        var nsName = IamService.GetNsName(iamId);
        var daemonSetment = await kubernetes.AppsV1
                .ReadNamespacedDaemonSetAsync(resName, nsName);
        if (daemonSetment == null)
        {
            return null;
        }

        var cmd = GetAppCreateCommand<DaemonSetEditCommand>(resName, daemonSetment);

        return cmd;
    }

    #endregion Details

    public override async Task<IEnumerable<DaemonSetDetails>> ListAsync
        (int iamId, string grpName, string? env = null)
    {
        var nsName = IamService.GetNsName(iamId);
        if (env != null)
        {
            return await ListOneClusterAsync(iamId, env, nsName, grpName);
        }

        List<DaemonSetDetails> res = new();
        foreach (var item in _k8sFactory.All)
        {
            var oneRes = await ListOneClusterAsync(iamId, item.Key, nsName, grpName);
            res.AddRange(oneRes);
        }
        return res;
    }

    private async Task<List<DaemonSetDetails>> ListOneClusterAsync
    (int iamId, string env, string nsName, string grpName)
    {
        var kubernetes = _k8sFactory.MustGet(env);
        var labelSelector = $"{K8sLabelsConstants.LabelIamId}={iamId}," +
           $"{K8sLabelsConstants.LabelEnv}={env}";

        var appLabelSelector = labelSelector +
            $",{K8sLabelsConstants.LabelGrpName}={grpName}";
        var podSelector = labelSelector +
            $",{K8sLabelsConstants.LabelApp}={grpName}-{env}";

        var deploys = await kubernetes.AppsV1
            .ListNamespacedDaemonSetAsync(
                nsName,
                labelSelector: appLabelSelector
                );

        if (deploys.Items.Count < 1)
        {
            return new List<DaemonSetDetails>();
        }

        var deploysList = deploys.Items;
        var result = new List<DaemonSetDetails>();
        var allPods = (
                await kubernetes.CoreV1
                    .ListNamespacedPodAsync(nsName,
                    labelSelector: podSelector // + $",{K8sLabelsConstants.LabelAppType}={DaemonSetEditCommand.Type}"

                    )
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

        return result.OrderBy(x => x.Name).ToList();
    }


    public override async Task PublishAsync(
        int iamId,
        AppPublishCommand command
        )
    {
        var kubernetes = _k8sFactory.MustGetByAppName(command.AppName);
        string daemonSetName = command.AppName;
        var nsName = IamService.GetNsName(iamId);
        var daemonSet = await kubernetes
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
        var patchResponse = await kubernetes
            .AppsV1
            .PatchNamespacedDaemonSetAsync(
            new V1Patch(patch, V1Patch.PatchType.JsonPatch),
            daemonSetName,
            nsName
            );
    }
}
