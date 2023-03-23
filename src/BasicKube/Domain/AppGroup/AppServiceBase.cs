using BasicKube.Api.Domain.Pod;
using System.Diagnostics;

namespace BasicKube.Api.Domain.AppGroup;

public abstract class AppServiceBase<TGrpInfo, TAppDetails, TEditCmd>
    : IAppService<TGrpInfo, TAppDetails, TEditCmd>
    where TAppDetails : AppDetailsQuery
    where TEditCmd : AppEditCommand
{
    protected readonly IamService IamService;

    protected AppServiceBase(IamService iamService)
    {
        IamService = iamService;
    }

    public abstract Task<IEnumerable<TGrpInfo>> ListGrpAsync(int iamId);

    public abstract Task<IEnumerable<TAppDetails>> ListAsync(int iamId, string appName, string? evn = null);


    public abstract Task CreateAsync(int iamId, TEditCmd cmd);

    public abstract Task UpdateAsync(int iamId, TEditCmd cmd);


    public abstract Task DelAsync(int iamId, string appName);

    public abstract Task<TEditCmd?> DetailsAsync(int iamId, string appName);

    public abstract Task PublishAsync(int iamId, AppPublishCommand cmd);


    #region internal

    public static TKubeObj CreateKubeApp<TKubeObj>(string nsName, AppEditCommand command)
        where TKubeObj : IKubernetesObject<V1ObjectMeta>, IValidate
    {
        var obj = Activator.CreateInstance<TKubeObj>();
        Debug.Assert(obj != null);
        if (command is DeployEditCommand deployCreateCmd)
        {
            var spec = new V1DeploymentSpec();
            (obj as V1Deployment)!.Spec = spec;
            spec.Selector = GetV1LabelSelector(nsName, command);
            spec.Template = PodService.GetPodTemplateSpec(nsName, command);

            spec.Replicas = deployCreateCmd.Replicas < 0
                ? 0 : deployCreateCmd.Replicas;
        }
        else if (command is DaemonSetEditCommand daemonSetCreateCommand)
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
        (string nsName, AppEditCommand command)
    {
        var metadata = new V1ObjectMeta
        {
            Name = command.AppName,
            NamespaceProperty = nsName,
            Annotations = new Dictionary<string, string>
            {
                [K8sLabelsConstants.LabelRegion] = command.Region,
                [K8sLabelsConstants.LabelRoom] = command.Room,
                [K8sLabelsConstants.LabelGrpName] = command.GrpName
            },
            Labels = new Dictionary<string, string>
            {
                [K8sLabelsConstants.LabelGrpName] = command.GrpName,
                [K8sLabelsConstants.LabelIamId] = command.IamId + "",
                [K8sLabelsConstants.LabelEnv] = command.Env
            }
        };

        //if (!string.IsNullOrWhiteSpace(command.TypeName))
        //{
        //    metadata.Labels.Add(K8sLabelsConstants.LabelAppType, command.TypeName);
        //}


        return metadata;
    }

    public static V1LabelSelector GetV1LabelSelector
        (string nsName, AppEditCommand command)
    {
        return new V1LabelSelector
        {
            MatchLabels = new Dictionary<string, string>
            {
                [K8sLabelsConstants.LabelApp] = command.AppName,
                //[K8sLabelsConstants.LabelAppType] = command.TypeName,
            }
        };
    }

    #endregion internal

    public static TCmd GetAppCreateCommand<TCmd>
        (string resName, IKubernetesObject<V1ObjectMeta> kubeApp)
        where TCmd : AppEditCommand
    {
        var obj = Activator.CreateInstance<TCmd>();
        Debug.Assert(obj != null);

        obj.GrpName = kubeApp.Metadata.Labels[K8sLabelsConstants.LabelGrpName];
        obj.AppName = resName;
        obj.Env = kubeApp.Metadata.Labels[K8sLabelsConstants.LabelEnv];
        obj.IamId = int.Parse(kubeApp.Metadata.Labels[K8sLabelsConstants.LabelIamId] ?? "0");
        obj.Region = kubeApp.Metadata.Annotations[K8sLabelsConstants.LabelRegion];
        obj.Room = kubeApp.Metadata.Annotations[K8sLabelsConstants.LabelRoom];

        if (obj is DeployEditCommand deployCreateCommand)
        {
            var app = kubeApp as V1Deployment;
            Debug.Assert(app != null);

            obj.Containers = PodService
                .GetContainerInfos(app.Spec.Template.Spec.Containers);
            deployCreateCommand.Replicas = app.Spec.Replicas ?? 0;
        }
        else if (obj is DaemonSetEditCommand)
        {
            var app = kubeApp as V1DaemonSet;
            Debug.Assert(app != null);

            obj.Containers = PodService
                .GetContainerInfos(app.Spec.Template.Spec.Containers);
        }

        return obj;
    }
}
