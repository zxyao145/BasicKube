using BasicKube.Api.Domain.Pod;
using Json.Patch;
using k8s;
using k8s.Autorest;
using System.Text.Json;

namespace BasicKube.Api.Domain.AppGroup;

public interface ICronJobAppService : IAppService<CronJobGrpInfo, CronJobDetails, CronJobEditCommand>
{
    public Task Suspend(int iamId, CronJobSuspendCommand command);
}

[Service<ICronJobAppService>]
public class CronJobAppService : AppServiceBase<CronJobGrpInfo, CronJobDetails, CronJobEditCommand>, ICronJobAppService
{
    private readonly KubernetesFactory _k8sFactory;
    private readonly ILogger<CronJobAppService> _logger;

    public CronJobAppService(
        KubernetesFactory kubernetes,
        ILogger<CronJobAppService> logger,
        IamService iamService) : base(iamService)
    {
        _k8sFactory = kubernetes;
        _logger = logger;
    }


    #region edit

    public override async Task CreateAsync(int iamId, CronJobEditCommand command)
    {
        var nsName = IamService.GetNsName(iamId);
        V1CronJob job = CmdToJob(nsName, command);
        await _k8sFactory.MustGet(command.Env)
            .BatchV1
            .CreateNamespacedCronJobAsync(job, nsName);
    }

    public override async Task UpdateAsync(int iamId, CronJobEditCommand command)
    {
        var nsName = IamService.GetNsName(iamId);
        var job = CmdToJob(nsName, command);
        await _k8sFactory.MustGet(command.Env)
            .BatchV1
            .ReplaceNamespacedCronJobAsync
                (job,
                    job.Metadata.Name,
                    job.Metadata.NamespaceProperty
                    );
    }

    private static V1CronJob CmdToJob(string nsName, CronJobEditCommand command)
    {
        command.RestartPolicy = "OnFailure"; // or Never
        var jobTemplate = command.JobTemplate;
        var job = new V1CronJob()
        {
            Metadata = PodService.CreateObjectMeta(nsName, command),
            Spec = new V1CronJobSpec()
            {
                Schedule = command.Schedule,
                SuccessfulJobsHistoryLimit = command.SuccessfulJobsHistoryLimit,
                FailedJobsHistoryLimit = command.FailedJobsHistoryLimit,
                StartingDeadlineSeconds = command.StartingDeadlineSeconds,
                ConcurrencyPolicy = command.ConcurrencyPolicy,
                Suspend = command.Suspend,
                JobTemplate = new V1JobTemplateSpec()
                {
                    Metadata = PodService.CreateObjectMeta(nsName, command),
                    Spec = new V1JobSpec()
                    {
                        // 不设置：不会删除；0: job 删除后立即删除；>0: 等待n秒后删除
                        TtlSecondsAfterFinished = 3600,
                        ActiveDeadlineSeconds = jobTemplate.ActiveDeadlineSeconds,
                        BackoffLimit = jobTemplate.BackoffLimit,
                        Completions = jobTemplate.Completions,
                        Parallelism = jobTemplate.Parallelism,
                        //Selector = PodService.GetV1LabelSelector(nsName, command),
                        Template = PodService.GetPodTemplateSpec(nsName, command),
                    }
                }

            }
        };
#if DEBUG
        var yaml = KubernetesYaml.Serialize(job);
#endif
        job.Validate();
        return job;
    }

    #endregion edit


    public override async Task DelAsync(int iamId, string resName)
    {
        var nsName = IamService.GetNsName(iamId);
        await _k8sFactory.MustGetByAppName(resName)
            .BatchV1
            .DeleteNamespacedCronJobAsync(resName, nsName);
        _logger.LogInformation("Del end:{0}", resName);
    }

    public override async Task<CronJobEditCommand?> DetailsAsync(int iamId, string resName)
    {
        var nsName = IamService.GetNsName(iamId);
        var job = await _k8sFactory.MustGetByAppName(resName)
            .BatchV1
            .ReadNamespacedCronJobAsync(resName, nsName);

        var cmd = new CronJobEditCommand();
        cmd.GrpName = job.Metadata.Labels[K8sLabelsConstants.LabelGrpName];
        cmd.Env = job.Metadata.Labels[K8sLabelsConstants.LabelEnv];
        cmd.AppName = resName;
        cmd.IamId = int.Parse(job.Metadata.Labels[K8sLabelsConstants.LabelIamId] ?? "0");
        cmd.Region = job.Metadata.Annotations[K8sLabelsConstants.LabelRegion];
        cmd.Room = job.Metadata.Annotations[K8sLabelsConstants.LabelRoom];


        cmd.Containers = PodService
            .GetContainerInfos(job.Spec.JobTemplate.Spec.Template.Spec.Containers);

        cmd.Schedule = job.Spec.Schedule;
        cmd.SuccessfulJobsHistoryLimit = job.Spec.SuccessfulJobsHistoryLimit;
        cmd.FailedJobsHistoryLimit = job.Spec.FailedJobsHistoryLimit;
        cmd.StartingDeadlineSeconds = job.Spec.StartingDeadlineSeconds;
        cmd.Suspend = job.Spec.Suspend ?? false;
        cmd.ConcurrencyPolicy = job.Spec.ConcurrencyPolicy;
        var jobSpec = job.Spec.JobTemplate.Spec;
        cmd.JobTemplate = new JobInfo()
        {
            ActiveDeadlineSeconds = jobSpec.ActiveDeadlineSeconds,
            BackoffLimit = jobSpec.BackoffLimit,
            Completions = jobSpec.Completions,
            Parallelism = jobSpec.Parallelism,
        };

        return cmd;
    }

    public override async Task<IEnumerable<CronJobDetails>> ListAsync
        (int iamId, string grpName, string? env = null)
    {
        var nsName = IamService.GetNsName(iamId);
        if (!string.IsNullOrWhiteSpace(env))
        {
            return await ListOneEnv(iamId, env, nsName, grpName);
        }

        var result = new List<CronJobDetails>();
        foreach (var item in _k8sFactory.All)
        {
            var temp = await ListOneEnv(iamId, item.Key, nsName, grpName);
            result.AddRange(temp);
        }
        return result;
    }


    private async Task<List<CronJobDetails>> ListOneEnv
        (int iamId, string env, string nsName, string grpName)
    {
        var labelSelector = $"{K8sLabelsConstants.LabelIamId}={iamId}" +
            $",{K8sLabelsConstants.LabelEnv}={env}";

        var appLabelSelector = labelSelector +
            $",{K8sLabelsConstants.LabelGrpName}={grpName}";
        var podSelector = labelSelector +
            $",{K8sLabelsConstants.LabelApp}={grpName}-{env}";

        var kubernetes = _k8sFactory.MustGet(env);
        var apps = await kubernetes
            .BatchV1
            .ListNamespacedCronJobAsync(
                nsName,
                labelSelector: appLabelSelector
                );

        if (apps.Items.Count < 1)
        {
            return new List<CronJobDetails>();
        }

        var appList = apps.Items;
        var result = new List<CronJobDetails>();

        foreach (var app in appList)
        {
            var curName = app.Metadata.Name;
            var jobSpec = app.Spec.JobTemplate.Spec;
            var relatedJobs = await GetRelatedJobs(
                kubernetes,
                nsName,
                appLabelSelector,
                $"{grpName}-{env}"
            );

            var details = new CronJobDetails
            {
                Name = curName,
                Suspend = app.Spec.Suspend ?? false,
                ActiveDeadlineSeconds = jobSpec.ActiveDeadlineSeconds,
                BackoffLimit = jobSpec.BackoffLimit,
                Completions = jobSpec.Completions,
                Parallelism = jobSpec.Parallelism,
                RestartPolicy = jobSpec.Template.Spec.RestartPolicy,
            };

            var historyGrp = relatedJobs
                .GroupBy(x => x.Failed == 0)
                .ToDictionary(x => x.Key, x => x);

            if (historyGrp.TryGetValue(true, out var v1))
            {
                details.SuccessHistory = v1.ToList();
            }
            if (historyGrp.TryGetValue(false, out var v2))
            {
                details.FailedHistory = v2.ToList();
            }

            result.Add(details);
        }

        return result.OrderBy(x => x.Name).ToList();
    }

    private async Task<List<CronJobHistoryInfo>> GetRelatedJobs(
        IKubernetes kubernetes,
        string nsName,
        string appLabelSelector,
        string cronJobName
        )
    {
        var relatedJobs = await kubernetes
            .BatchV1
            .ListNamespacedJobAsync(
                nsName,
                labelSelector: appLabelSelector + $",{K8sLabelsConstants.LabelAppType}={CronJobEditCommand.Type}"
            );
        var res = new List<CronJobHistoryInfo>();
        foreach (var relatedJobsItem in relatedJobs.Items)
        {
            var status = relatedJobsItem.Status;
            var historyInfo = new CronJobHistoryInfo()
            {
                JobName = relatedJobsItem.Metadata.Name,
                StartTime = status.StartTime,
                Ready = status.Ready ?? 0,
                Failed = status.Failed ?? 0,
                Succeeded = status.Succeeded ?? 0,
                Completions = relatedJobsItem.Spec.Completions ?? 1,
                Suspend = relatedJobsItem.Spec.Suspend
            };
            res.Add(historyInfo);
        }

        return res;
    }


    public override async Task<IEnumerable<CronJobGrpInfo>> ListGrpAsync(int iamId)
    {
        var label = $"{K8sLabelsConstants.LabelIamId}={iamId}";

        var res = new List<V1CronJob>();
        foreach (var item in _k8sFactory.All)
        {
            var jobs = await item.Value
                .BatchV1
            .ListNamespacedCronJobAsync(
            IamService.GetNsName(iamId),
            labelSelector: label + $",{K8sLabelsConstants.LabelEnv}={item.Key}"
            );

            res.AddRange(jobs.Items);
        }


        return res
            .Select(x => x.Metadata.Labels[K8sLabelsConstants.LabelGrpName])
            .ToHashSet()
            .Select(x => new CronJobGrpInfo()
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
            .ReadNamespacedCronJobAsync(dnName, nsName);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
        var old = JsonSerializer.SerializeToDocument(app, options);

        var imgName = app.Spec.JobTemplate.Spec.Template.Spec.Containers[0].Image.Split(":")[0];
        var newImg = $"{imgName}:{command.Tag}";
        app.Spec.JobTemplate.Spec.Template.Spec.Containers[0].Image = newImg;
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

    public async Task Suspend(int iamId, CronJobSuspendCommand command)
    {
        var kubernetes = _k8sFactory.MustGetByAppName(command.AppName);
        string dnName = command.AppName;
        var nsName = IamService.GetNsName(iamId);
        var app = await kubernetes.BatchV1
            .ReadNamespacedCronJobAsync(dnName, nsName);
        if (app.Spec.Suspend == command.Suspend)
        {
            return;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        var old = JsonSerializer.SerializeToDocument(app, options);

        app.Spec.Suspend = command.Suspend;

        var expected = JsonSerializer.SerializeToDocument(app);
        // JsonPatch.Net
        var patch = old.CreatePatch(expected);
        await kubernetes.BatchV1
            .PatchNamespacedCronJobAsync(
                new V1Patch(patch, V1Patch.PatchType.JsonPatch),
                dnName,
                nsName
            );
    }
}
