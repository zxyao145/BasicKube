using BasicKube.Api.Common;
using BasicKube.Api.Domain.Pod;
using Json.Patch;
using System.Text.Json;

namespace BasicKube.Api.Domain.App;

public class DaemonSetAppService : AppServiceBase<DaemonSetDetails, DaemonSetCreateCommand>
{
    private readonly IKubernetes _kubernetes;

    private readonly ILogger<DaemonSetAppService> _logger;

    public DaemonSetAppService(IamService iamService, IKubernetes kubernetes, ILogger<DaemonSetAppService> logger) : base(iamService)
    {
        _kubernetes = kubernetes;
        _logger = logger;
    }

    #region edit

    public override async Task CreateAsync(int iamId, DaemonSetCreateCommand command)
    {
        var nsName = IamService.GetNsName(iamId);
        var v1DaemonSet = CreateKubeApp<V1DaemonSet>(nsName, command);
        await _kubernetes.AppsV1
            .CreateNamespacedDaemonSetAsync(v1DaemonSet, v1DaemonSet.Metadata.NamespaceProperty);
    }


    public override async Task UpdateAsync(int iamId, DaemonSetCreateCommand command)
    {
        var nsName = IamService.GetNsName(iamId);
        var v1DaemonSet = CreateKubeApp<V1DaemonSet>(nsName, command);
        await _kubernetes.AppsV1
            .ReplaceNamespacedDaemonSetAsync(v1DaemonSet, v1DaemonSet.Metadata.Name,
            v1DaemonSet.Metadata.NamespaceProperty
            );
    }

    #endregion

    public override async Task DelAsync(int iamId, string deployUnitName)
    {
        var nsName = IamService.GetNsName(iamId);
        await _kubernetes.AppsV1.DeleteNamespacedDaemonSetAsync(deployUnitName, nsName);
    }

    #region Details

    public override async Task<DaemonSetCreateCommand?> DetailsAsync(int iamId, string deployUnitName)
    {
        var nsName = IamService.GetNsName(iamId);
        var daemonSetment = await _kubernetes.AppsV1
                .ReadNamespacedDaemonSetAsync(deployUnitName, nsName);
        if (daemonSetment == null)
        {
            return null;
        }

        var cmd = GetAppCreateCommand<DaemonSetCreateCommand>(deployUnitName, daemonSetment);

        return cmd;
    }

    #endregion

    public override async Task<IEnumerable<DaemonSetDetails>> ListAsync
        (int iamId, string appName, string? env = null)
    {
        var nsName = IamService.GetNsName(iamId);
        var labelSelector = $"{Constants.LableIamId}={iamId}," +
            $"{Constants.LableAppName}={appName}," +
            $"{Constants.LableDeployUnitType}={DaemonSetCreateCommand.Type}";
        if (!string.IsNullOrWhiteSpace(env))
        {
            labelSelector += $",{Constants.LableEnv}={env}";
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
                    labelSelector: $"{Constants.LableIamId}={iamId},{Constants.LableDeployUnitType}={DaemonSetCreateCommand.Type}")
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
        string daemonSetName = command.DeployUnitName;
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
