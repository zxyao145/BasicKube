using BasicKube.Api.Common;
using BasicKube.Api.Domain.Pod;
using BasicKube.Api.Exceptions;
using Json.Patch;
using k8s.Models;
using System.Text.Json;
using System.Xml.Linq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace BasicKube.Api.Domain.App;

public interface IDeployAppService : IAppService<DeployGrpInfo, DeployDetails, DeployEditCommand>
{

}

[Service<IDeployAppService>]
public class DeployAppService 
    : AppServiceBase<DeployGrpInfo, DeployDetails, DeployEditCommand>, IDeployAppService
{
    private readonly KubernetesFactory _k8sFactory;

    private readonly ILogger<DeployAppService> _logger;

    public DeployAppService(IamService iamService, KubernetesFactory kubernetes, ILogger<DeployAppService> logger) : base(iamService)
    {
        _k8sFactory = kubernetes;
        _logger = logger;
    }

    #region edit

    public override async Task CreateAsync(int iamId, DeployEditCommand command)
    {
        var nsName = IamService.GetNsName(iamId);
        var v1Deploy = CreateKubeApp<V1Deployment>(nsName, command);
        await _k8sFactory.MustGet(command.Env).AppsV1
               .CreateNamespacedDeploymentAsync(v1Deploy, v1Deploy.Metadata.NamespaceProperty);
    }


    public override async Task UpdateAsync(int iamId, DeployEditCommand command)
    {
        var nsName = IamService.GetNsName(iamId);
        var v1Deploy = CreateKubeApp<V1Deployment>(nsName, command);
        await _k8sFactory.MustGet(command.Env).AppsV1
            .ReplaceNamespacedDeploymentAsync(
                v1Deploy, v1Deploy.Metadata.Name,
                v1Deploy.Metadata.NamespaceProperty
            );
    }

    #endregion

    public override async Task DelAsync(int iamId, string resName)
    {
        var nsName = IamService.GetNsName(iamId);
        await _k8sFactory.MustGetByAppName(resName)
            .AppsV1.DeleteNamespacedDeploymentAsync(resName, nsName);
    }

    #region Details

    public override async Task<DeployEditCommand?> DetailsAsync(int iamId, string resName)
    {
        var nsName = IamService.GetNsName(iamId);
        var deployment = await _k8sFactory.MustGetByAppName(resName)
            .AppsV1
            .ReadNamespacedDeploymentAsync(resName, nsName);
        if (deployment == null)
        {
            return null;
        }

        var cmd = GetAppCreateCommand<DeployEditCommand>(resName, deployment);

        return cmd;
    }

    #endregion


    public override async Task<IEnumerable<DeployGrpInfo>> ListGrpAsync(int iamId)
    {
        var nsName = IamService.GetNsName(iamId);
        var label = $"{K8sLabelsConstants.LabelIamId}={iamId}";

        var res = new List<V1Deployment>();
        foreach (var item in _k8sFactory.All)
        {
            var env = item.Key;
            var deploymentListV1 = await item.Value
                .AppsV1
                .ListNamespacedDeploymentAsync(
                   nsName, 
                   labelSelector: label + $",{K8sLabelsConstants.LabelEnv}={env}"
                );
            res.AddRange(deploymentListV1.Items);
        }

        var appNames = res
            .Select(x =>
            {
                if (x.Metadata.Labels.ContainsKey(K8sLabelsConstants.LabelGrpName))
                {
                    return x.Metadata.Labels[K8sLabelsConstants.LabelGrpName];
                }

                return "";
            })
            .ToHashSet()
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => new DeployGrpInfo()
            {
                Name = x
            })
            .ToList();
        return appNames;
    }


    public override async Task<IEnumerable<DeployDetails>> ListAsync
        (int iamId, string grpName, string? env = null)
    {
        var nsName = IamService.GetNsName(iamId);
        if(!string.IsNullOrWhiteSpace(env))
        {
            return await ListOneEnv(iamId, env, nsName, grpName);
        }

        var result = new List<DeployDetails>();
        foreach (var item in _k8sFactory.All)
        {
            var temp = await ListOneEnv(iamId, item.Key, nsName, grpName);
            result.AddRange(temp);
        }
        return result;
    }

    private async Task<List<DeployDetails>> ListOneEnv
        (int iamId, string env, string nsName, string grpName)
    {
        var labelSelector = $"{K8sLabelsConstants.LabelIamId}={iamId}" +
            $",{K8sLabelsConstants.LabelEnv}={env}"; ;

        var appLabelSelector = labelSelector;
        var podSelector = labelSelector;

        if (!string.IsNullOrEmpty(grpName))
        {
            appLabelSelector += $",{K8sLabelsConstants.LabelGrpName}={grpName}";
            podSelector += $",{K8sLabelsConstants.LabelApp}={grpName}-{env}";
        }

        var kubernetes = _k8sFactory.MustGet(env);
        var deploys = await kubernetes.AppsV1
            .ListNamespacedDeploymentAsync(nsName, labelSelector: appLabelSelector);

        if (deploys.Items.Count < 1)
        {
            return new List<DeployDetails>();
        }

        var deploysList = deploys.Items;
        var result = new List<DeployDetails>();
        var allPods = (
                await kubernetes
                .CoreV1
                .ListNamespacedPodAsync(
                    nsName,
                    labelSelector: podSelector // + $",{K8sLabelsConstants.LabelAppType}={DeployEditCommand.Type}"
                    )
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
                .OrderByDescending(x=>x.StartTime)
                .ThenBy(x => x.Name)
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
        string appName = command.AppName;
        var kubernetes = _k8sFactory.MustGetByAppName(appName);

        var nsName = IamService.GetNsName(iamId);
        // https://github.com/kubernetes-client/csharp/blob/f615b5b4595a35aa6ddc5fed3c396cabdc3f3efa
        // /examples/restart/Program.cs#L25
        var deployment = await kubernetes.AppsV1
            .ReadNamespacedDeploymentAsync(appName, nsName);
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
        await kubernetes.AppsV1
            .PatchNamespacedDeploymentAsync(
            new V1Patch(patch, V1Patch.PatchType.JsonPatch),
            appName,
            nsName
            );
    }
}
