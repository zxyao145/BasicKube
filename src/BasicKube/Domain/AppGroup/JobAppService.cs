using BasicKube.Api.Domain.App;
using BasicKube.Api.Domain.Pod;
using Json.Patch;
using k8s.Autorest;
using System.Text.Json;

namespace BasicKube.Api.Domain.AppGroup;

public interface IJobAppService : IAppService<JobGrpInfo, JobDetails, JobEditCommand>
{

}

[Service<IJobAppService>]
public class JobAppService : AppServiceBase<JobGrpInfo, JobDetails, JobEditCommand>, IJobAppService
{
    private readonly KubernetesFactory _k8sFactory;
    private readonly ILogger<JobAppService> _logger;

    public JobAppService(
        KubernetesFactory kubernetes,
        ILogger<JobAppService> logger,
        IamService iamService) : base(iamService)
    {
        _k8sFactory = kubernetes;
        _logger = logger;
    }


    #region edit

    public override async Task CreateAsync(int iamId, JobEditCommand command)
    {
        var nsName = IamService.GetNsName(iamId);
        V1Job job = CmdToJob(nsName, command);
        await _k8sFactory.MustGet(command.Env)
            .BatchV1
            .CreateNamespacedJobAsync(job, nsName);
    }

    public override async Task UpdateAsync(int iamId, JobEditCommand command)
    {
        var nsName = IamService.GetNsName(iamId);
        V1Job job = CmdToJob(nsName, command);
        try
        {
            await _k8sFactory.MustGet(command.Env)
                .BatchV1
                .DeleteNamespacedJobAsync(
                    job.Metadata.Name,
                    job.Metadata.NamespaceProperty
            );
        }
        catch (HttpOperationException e)
        {
            if(((int)e.Response.StatusCode) > 499) throw;
        }
        
        await _k8sFactory.MustGet(command.Env)
            .BatchV1.CreateNamespacedJobAsync(job, nsName);
    }

    private static V1Job CmdToJob(string nsName, JobEditCommand command)
    {
        command.RestartPolicy = "OnFailure"; // or Never
        var job = new V1Job
        {
            Metadata = PodService.CreateObjectMeta(nsName, command),
            Spec = new V1JobSpec
            {
                // 不设置：不会删除；0: job 删除后立即删除；>0: 等待n秒后删除
                TtlSecondsAfterFinished = 3600, 
                ActiveDeadlineSeconds = command.ActiveDeadlineSeconds,
                BackoffLimit = command.BackoffLimit,
                Completions = command.Completions,
                Parallelism = command.Parallelism,
                //Selector = PodService.GetV1LabelSelector(nsName, command),
                Template = PodService.GetPodTemplateSpec(nsName, command)
            }
        };
#if DEBUG
        var yaml = KubernetesYaml.Serialize(job);
#endif
        job.Validate();
        return job;
    }

    #endregion


    public override async Task DelAsync(int iamId, string resName)
    {
        var nsName = IamService.GetNsName(iamId);
        await _k8sFactory.MustGetByAppName(resName)
            .BatchV1.DeleteNamespacedJobAsync(resName, nsName);
        _logger.LogInformation("Del end:{0}", resName);
    }

    public override async Task<JobEditCommand?> DetailsAsync(int iamId, string resName)
    {
        var nsName = IamService.GetNsName(iamId);
        var job = await _k8sFactory.MustGetByAppName(resName)
            .BatchV1
            .ReadNamespacedJobAsync(resName, nsName);

        var cmd = new JobEditCommand();
        cmd.GrpName = job.Metadata.Labels[K8sLabelsConstants.LabelGrpName];
        cmd.Env = job.Metadata.Labels[K8sLabelsConstants.LabelEnv];
        cmd.AppName = resName;
        cmd.IamId = int.Parse(job.Metadata.Labels[K8sLabelsConstants.LabelIamId] ?? "0");
        cmd.Region = job.Metadata.Annotations[K8sLabelsConstants.LabelRegion];
        cmd.Room = job.Metadata.Annotations[K8sLabelsConstants.LabelRoom];


        cmd.Containers = PodService.GetContainerInfos(job.Spec.Template.Spec.Containers);
        cmd.ActiveDeadlineSeconds = job.Spec.ActiveDeadlineSeconds;
        cmd.BackoffLimit = job.Spec.BackoffLimit;
        cmd.Completions = job.Spec.Completions;
        cmd.Parallelism = job.Spec.Parallelism;
        cmd.RestartPolicy = job.Spec.Template.Spec.RestartPolicy;
        return cmd;
    }

    public override async Task<IEnumerable<JobDetails>> ListAsync
        (int iamId, string grpName, string? env = null)
    {
        var nsName = IamService.GetNsName(iamId);
        if (!string.IsNullOrWhiteSpace(env))
        {
            return await ListOneEnv(iamId, env, nsName, grpName);
        }

        var result = new List<JobDetails>();
        foreach (var item in _k8sFactory.All)
        {
            var temp = await ListOneEnv(iamId, item.Key, nsName, grpName);
            result.AddRange(temp);
        }
        return result;

    }


    private async Task<List<JobDetails>> ListOneEnv
        (int iamId, string env, string nsName, string grpName)
    {
        var labelSelector = $"{K8sLabelsConstants.LabelIamId}={iamId}" +
            $",{K8sLabelsConstants.LabelEnv}={env}";

        var appLabelSelector = labelSelector +
            $",{K8sLabelsConstants.LabelGrpName}={grpName}";
        var podSelector = labelSelector +
            $",{K8sLabelsConstants.LabelApp}={grpName}-{env}";

        var kubernetes = _k8sFactory.MustGet(env);
        var apps = await kubernetes.BatchV1
            .ListNamespacedJobAsync(
                nsName,
                labelSelector: appLabelSelector // + $",{K8sLabelsConstants.LabelAppType}={DeployEditCommand.Type}"
                );

        if (apps.Items.Count < 1)
        {
            return new List<JobDetails>();
        }

        var appList = apps.Items;
        var result = new List<JobDetails>();
        var allPods = (
                await kubernetes.CoreV1
                    .ListNamespacedPodAsync(nsName,
                    labelSelector: podSelector
                    )
            ).Items
            .OrderBy(x => x.Status.StartTime)
            .ThenBy(x => x.Metadata.Name)
            .ToList();

        foreach (var app in appList)
        {
            var curName = app.Metadata.Name;
            var details = new JobDetails
            {
                Name = curName,
                ActiveDeadlineSeconds = app.Spec.ActiveDeadlineSeconds,
                BackoffLimit = app.Spec.BackoffLimit,
                Completions = app.Spec.Completions,
                Parallelism = app.Spec.Parallelism,
                RestartPolicy = app.Spec.Template.Spec.RestartPolicy
            };
            var count = allPods.Count;

            for (int i = count - 1; i > -1; i--)
            {
                var curPod = allPods[i];
                var podName = curPod.Metadata.Name;
                if (podName.StartsWith(curName))
                {
                    allPods.RemoveAt(i);
                    var podDetail = PodService.GetPodDetail(curPod);
                    details.PodDetails.Add(podDetail);
                }
            }

            details.PodDetails = details.PodDetails
                .OrderByDescending(x => x.StartTime)
                .ThenBy(x => x.Name)
                .ToList();
            result.Add(details);
        }

        return result.OrderBy(x => x.Name).ToList();
    }



    public override async Task<IEnumerable<JobGrpInfo>> ListGrpAsync(int iamId)
    {
        var label = $"{K8sLabelsConstants.LabelIamId}={iamId}";

        var res = new List<V1Job>();
        foreach (var item in _k8sFactory.All)
        {
            var jobs = await item.Value.BatchV1
            .ListNamespacedJobAsync(
            IamService.GetNsName(iamId),
            labelSelector: label + $",{K8sLabelsConstants.LabelEnv}={item.Key}"
            );

            res.AddRange(jobs.Items);
        }
        

        return res
            .Select(x => x.Metadata.Labels[K8sLabelsConstants.LabelGrpName])
            .ToHashSet()
            .Select(x => new JobGrpInfo()
            {
                Name = x
            });
    }


    public override async Task PublishAsync(
        int iamId,
        AppPublishCommand command
        )
    {
        var kubernetes = _k8sFactory.MustGetByAppName(command.AppName);
        string dnName = command.AppName;
        var nsName = IamService.GetNsName(iamId);
        var app = await kubernetes.BatchV1
            .ReadNamespacedJobAsync(dnName, nsName);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
        var old = JsonSerializer.SerializeToDocument(app, options);

        var imgName = app.Spec.Template.Spec.Containers[0].Image.Split(":")[0];
        var newImg = $"{imgName}:{command.Tag}";
        app.Spec.Template.Spec.Containers[0].Image = newImg;
        app.Metadata.Annotations.Add("kubernetes.io/change-cause", command.Description ?? "");

        var expected = JsonSerializer.SerializeToDocument(app);
        // JsonPatch.Net
        var patch = old.CreatePatch(expected);
        await kubernetes.BatchV1
            .PatchNamespacedJobAsync(
            new V1Patch(patch, V1Patch.PatchType.JsonPatch),
            dnName,
            nsName
            );
    }
}
