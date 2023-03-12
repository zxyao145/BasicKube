using BasicKube.Api.Common;
using BasicKube.Api.Domain.Pod;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.SignalR;
using Org.BouncyCastle.Security;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using static KubeClient.K8sAnnotations;

namespace BasicKube.Api.Domain.App;

public abstract class AppServiceBase<TAppDetails, TEditCmd>
    : IAppService<TAppDetails, TEditCmd>
    where TAppDetails : AppDetailsQuery
    where TEditCmd : AppCreateCommand
{

    protected readonly IamService IamService;

    protected AppServiceBase(IamService iamService)
    {
        IamService = iamService;
    }

    public abstract Task CreateAsync(int iamId, TEditCmd cmd);
    public abstract Task DelAsync(int iamId, string appName);
    public abstract Task<TEditCmd?> DetailsAsync(int iamId, string appName);
    public abstract Task<IEnumerable<TAppDetails>> ListAsync(int iamId, string appName, string? evn = null);
    public abstract Task PublishAsync(int iamId, AppPublishCommand cmd);
    public abstract Task UpdateAsync(int iamId, TEditCmd cmd);

    #region CreateKubeApp

    public static TKubeObj CreateKubeApp<TKubeObj>(string nsName, AppCreateCommand command)
        where TKubeObj : IKubernetesObject<V1ObjectMeta>, IValidate
    {
        var obj = Activator.CreateInstance<TKubeObj>();
        Debug.Assert(obj != null);
        if (command is DeployCreateCommand deployCreateCmd)
        {
            var spec = new V1DeploymentSpec();
            (obj as V1Deployment)!.Spec = spec;
            spec.Selector = GetV1LabelSelector(nsName, command);
            spec.Template = PodService.GetPodTemplateSpec(nsName, command);

            spec.Replicas = deployCreateCmd.Replicas < 0
                ? 0 : deployCreateCmd.Replicas;
        }
        else if (command is DaemonSetCreateCommand daemonSetCreateCommand)
        {
            var spec = new V1DaemonSetSpec();
            (obj as V1DaemonSet)!.Spec = spec;
            spec.Selector = GetV1LabelSelector(nsName, command);
            spec.Template = PodService.GetPodTemplateSpec(nsName, command);
        }

        Debug.Assert(obj != null);
        obj.Metadata = CreateObjectMeta(nsName, command);

        obj.Validate();
#if DEBUG
        var yaml = KubernetesYaml.Serialize(obj);
#endif
        return obj;
    }

    public static V1ObjectMeta CreateObjectMeta
        (string nsName, AppCreateCommand command)
    {
        var metadata = new V1ObjectMeta
        {
            Name = command.AppName,
            NamespaceProperty = nsName,
            Annotations = new Dictionary<string, string>
            {
                [Constants.LableRegion] = command.Region,
                [Constants.LableRoom] = command.Room,
                [Constants.LableAppGrpName] = command.GrpName
            },
            Labels = new Dictionary<string, string>
            {
                [Constants.LableAppGrpName] = command.GrpName,
                [Constants.LableIamId] = command.IamId + "",
                [Constants.LableEnv] = command.Env
            }
        };

        if (!string.IsNullOrWhiteSpace(command.TypeName))
        {
            metadata.Labels.Add(Constants.LableAppType, command.TypeName);
        }


        return metadata;
    }

    public static V1LabelSelector GetV1LabelSelector
        (string nsName, AppCreateCommand command)
    {
        return new V1LabelSelector
        {
            MatchLabels = new Dictionary<string, string>
            {
                [Constants.LableApp] = command.AppName,
                [Constants.LableAppType] = command.TypeName,
            }
        };
    }

    #endregion

    public static TCmd GetAppCreateCommand<TCmd>
        (string resName, IKubernetesObject<V1ObjectMeta> kubeApp)
        where TCmd : AppCreateCommand
    {
        var obj = Activator.CreateInstance<TCmd>();
        Debug.Assert(obj != null);

        obj.GrpName = kubeApp.Metadata.Labels[Constants.LableAppGrpName];
        obj.AppName = resName;
        obj.Env = kubeApp.Metadata.Labels[Constants.LableEnv];
        obj.IamId = int.Parse(kubeApp.Metadata.Labels[Constants.LableIamId] ?? "0");
        obj.Region = kubeApp.Metadata.Annotations[Constants.LableRegion];
        obj.Room = kubeApp.Metadata.Annotations[Constants.LableRoom];
        
        if(obj is DeployCreateCommand deployCreateCommand)
        {
            var app = kubeApp as V1Deployment;
            Debug.Assert(app != null);

            obj.Containers = PodService
                .GetContainerInfos(app.Spec.Template.Spec.Containers);
            deployCreateCommand.Replicas = app.Spec.Replicas ?? 0;
        }
        else if (obj is DaemonSetCreateCommand)
        {
            var app = kubeApp as V1DaemonSet;
            Debug.Assert(app != null);

            obj.Containers = PodService
                .GetContainerInfos(app.Spec.Template.Spec.Containers);
        }

        return obj;
    }
}
