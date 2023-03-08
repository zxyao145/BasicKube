using BasicKube.Api.Common;
using BasicKube.Api.Domain.Pod;
using Json.Patch;
using Org.BouncyCastle.Security;
using System.Text.Json;
using System.Xml.Linq;

namespace BasicKube.Api.Domain.App;

public class DeployAppService : AppServiceBase<DeployDetails, DeployCreateCommand>
{
    private readonly IKubernetes _kubernetes;

    private readonly ILogger<DeployAppService> _logger;

    public DeployAppService(IamService iamService, IKubernetes kubernetes, ILogger<DeployAppService> logger) : base(iamService)
    {
        _kubernetes = kubernetes;
        _logger = logger;
    }

    #region edit

    public override async Task CreateAsync(int iamId, DeployCreateCommand command)
    {
        var nsName = IamService.GetNsName(iamId);
        var v1Deploy = CreateKubeApp<V1Deployment>(nsName, command);
        await _kubernetes.AppsV1
               .CreateNamespacedDeploymentAsync(v1Deploy, v1Deploy.Metadata.NamespaceProperty);
    }


    public override async Task UpdateAsync(int iamId, DeployCreateCommand command)
    {
        var nsName = IamService.GetNsName(iamId);
        var v1Deploy = CreateKubeApp<V1Deployment>(nsName, command);
        await _kubernetes.AppsV1
            .ReplaceNamespacedDeploymentAsync(
                v1Deploy, v1Deploy.Metadata.Name,
                v1Deploy.Metadata.NamespaceProperty
            );
    }

    #endregion

    public override async Task DelAsync(int iamId, string deployUnitName)
    {
        var nsName = IamService.GetNsName(iamId);
        await _kubernetes.AppsV1.DeleteNamespacedDeploymentAsync(deployUnitName, nsName);
    }

    #region Details

    public override async Task<DeployCreateCommand?> DetailsAsync(int iamId, string deployUnitName)
    {
        var nsName = IamService.GetNsName(iamId);
        var deployment = await _kubernetes.AppsV1
            .ReadNamespacedDeploymentAsync(deployUnitName, nsName);
        if (deployment == null)
        {
            return null;
        }

        var cmd = GetAppCreateCommand<DeployCreateCommand>(deployUnitName, deployment);

        return cmd;
    }

    #endregion

    public override async Task<IEnumerable<DeployDetails>> ListAsync
        (int iamId, string appName, string? env = null)
    {
        var nsName = IamService.GetNsName(iamId);
        var labelSelector = $"{Constants.LableIamId}={iamId}," +
            $"{Constants.LableAppGrpName}={appName}," +
            $"{Constants.LableAppType}={DeployCreateCommand.Type}";
        if (!string.IsNullOrWhiteSpace(env))
        {
            labelSelector += $",{Constants.LableEnv}={env}";
        }

        var deploys = await _kubernetes.AppsV1
            .ListNamespacedDeploymentAsync(
                nsName,
                labelSelector: labelSelector
                );

        if (deploys.Items.Count < 1)
        {
            return new List<DeployDetails>();
        }

        var deploysList = deploys.Items;
        var result = new List<DeployDetails>();
        var allPods = (
                await _kubernetes.CoreV1
                    .ListNamespacedPodAsync(nsName, labelSelector: $"{Constants.LableIamId}={iamId},{Constants.LableAppType}={DeployCreateCommand.Type}")
            ).Items
            .OrderBy(x => x.Status.StartTime)
            .ThenBy(x => x.Metadata.Name)
            .ToList();

        foreach (var deploy in deploysList)
        {
            var curDeployName = deploy.Metadata.Name;
            var details = new DeployDetails
            {
                Name = curDeployName,
                Replicas = deploy.Status.Replicas ?? 0,
                ReadyReplicas = deploy.Status.ReadyReplicas ?? 0
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
        string dnName = command.AppName;
        var nsName = IamService.GetNsName(iamId);
        // https://github.com/kubernetes-client/csharp/blob/f615b5b4595a35aa6ddc5fed3c396cabdc3f3efa
        // /examples/restart/Program.cs#L25
        var deployment = await _kubernetes.AppsV1
            .ReadNamespacedDeploymentAsync(dnName, nsName);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
        var old = JsonSerializer.SerializeToDocument(deployment, options);


        var imgName = deployment.Spec.Template.Spec.Containers[0].Image.Split(":")[0];
        var newImg = $"{imgName}:{command.Tag}";
        deployment.Spec.Template.Spec.Containers[0].Image = newImg;
        // kubectl rollout history deploy deployName
        deployment.Metadata.Annotations.Add("kubernetes.io/change-cause", command.Description ?? "");

        var expected = JsonSerializer.SerializeToDocument(deployment);
        // JsonPatch.Net
        var patch = old.CreatePatch(expected);
        await _kubernetes.AppsV1
            .PatchNamespacedDeploymentAsync(
            new V1Patch(patch, V1Patch.PatchType.JsonPatch),
            dnName,
            nsName
            );
    }

}
