using BasicKube.Api.Common;
using BasicKube.Api.Domain.App;
using BasicKube.Api.Domain.Pod;
using Json.Patch;
using k8s;
using k8s.Autorest;
using System.Text.Json;
using System.Xml.Linq;

namespace BasicKube.Api.Domain.AppGroup;

public interface IResService<TGrpInfo, TResDetails, TEditCmd>
{
    /// <summary>
    /// 列出服务组简介列表
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="appName"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    public Task<IEnumerable<TGrpInfo>> ListGrpAsync(int iamId);


    /// <summary>
    /// 列出服务组详情列表
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="svcGrpName"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    public Task<IEnumerable<TResDetails>> ListAsync(int iamId, string? grpName, string? env = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="cmd"></param>
    /// <returns></returns>
    public Task CreateAsync(int iamId, TEditCmd cmd);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="cmd"></param>
    /// <returns></returns>
    public Task UpdateAsync(int iamId, TEditCmd cmd);

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="svcName"></param>
    /// <returns></returns>
    public Task DelAsync(int iamId, string resName);

    /// <summary>
    /// 资源详情
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="svcName"></param>
    /// <returns></returns>
    public Task<TEditCmd?> DetailsAsync(int iamId, string svcName);

    /// <summary>
    /// 更新应用
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="cmd"></param>
    /// <returns></returns>
    public Task PublishAsync(int iamId, AppPublishCommand cmd);
}

public interface IJobAppService : IResService<JobGrpInfo, JobDetails, JobEditCommand>
{

}

public class JobAppService : IJobAppService
{
    private readonly IKubernetes _kubernetes;
    private readonly ILogger<JobAppService> _logger;
    private readonly IamService _iamService;

    public JobAppService(
        IKubernetes kubernetes,
        ILogger<JobAppService> logger,
        IamService iamService)
    {
        _kubernetes = kubernetes;
        _logger = logger;
        _iamService = iamService;
    }


    #region edit

    public async Task CreateAsync(int iamId, JobEditCommand command)
    {
        var nsName = _iamService.GetNsName(iamId);
        V1Job job = CmdToJob(nsName, command);
        await _kubernetes.BatchV1.CreateNamespacedJobAsync(job, nsName);
    }

    public async Task UpdateAsync(int iamId, JobEditCommand command)
    {
        var nsName = _iamService.GetNsName(iamId);
        V1Job job = CmdToJob(nsName, command);
        //var oldJob = await _kubernetes.BatchV1
        //    .ReadNamespacedJobAsync(job.Metadata.Name, job.Metadata.NamespaceProperty);
        //job.Spec.Selector = oldJob.Spec.Selector;
        //job.Metadata.Uid = oldJob.Metadata.Uid;
        //job.Spec.Template.Metadata.Labels["controller-uid"] =
        //    oldJob.Spec.Selector.MatchLabels["controller-uid"];
        //job.Spec.Template.Metadata.Labels["job-name"] = command.AppName;
        //await _kubernetes.BatchV1
        //    .ReplaceNamespacedJobAsync(
        //        job,
        //        job.Metadata.Name,
        //        job.Metadata.NamespaceProperty
        //    );
        try
        {
            await _kubernetes.BatchV1.DeleteNamespacedJobAsync(
                job.Metadata.Name,
                job.Metadata.NamespaceProperty
            );
        }
        catch (HttpOperationException e)
        {
            if(((int)e.Response.StatusCode) > 499) throw;
        }
        
        await _kubernetes.BatchV1.CreateNamespacedJobAsync(job, nsName);
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


    public async Task DelAsync(int iamId, string resName)
    {
        var nsName = _iamService.GetNsName(iamId);
        await _kubernetes.BatchV1.DeleteNamespacedJobAsync(resName, nsName);
        _logger.LogInformation("Del end:{0}", resName);
    }

    public async Task<JobEditCommand?> DetailsAsync(int iamId, string resName)
    {
        var nsName = _iamService.GetNsName(iamId);
        var job = await _kubernetes.BatchV1
            .ReadNamespacedJobAsync(resName, nsName);

        var cmd = new JobEditCommand();
        cmd.GrpName = job.Metadata.Labels[Constants.LableAppGrpName];
        cmd.Env = job.Metadata.Labels[Constants.LableEnv];
        cmd.AppName = resName;
        cmd.IamId = int.Parse(job.Metadata.Labels[Constants.LableIamId] ?? "0");
        cmd.Region = job.Metadata.Annotations[Constants.LableRegion];
        cmd.Room = job.Metadata.Annotations[Constants.LableRoom];


        cmd.Containers = PodService.GetContainerInfos(job.Spec.Template.Spec.Containers);
        cmd.ActiveDeadlineSeconds = job.Spec.ActiveDeadlineSeconds;
        cmd.BackoffLimit = job.Spec.BackoffLimit;
        cmd.Completions = job.Spec.Completions;
        cmd.Parallelism = job.Spec.Parallelism;
        cmd.RestartPolicy = job.Spec.Template.Spec.RestartPolicy;
        return cmd;
    }

    public async Task<IEnumerable<JobDetails>> ListAsync(int iamId, string? grpName, string? env = null)
    {
        var nsName = _iamService.GetNsName(iamId);
        var labelSelector = $"{Constants.LableIamId}={iamId}," +
            $"{Constants.LableAppGrpName}={grpName}," +
            $"{Constants.LableAppType}={JobEditCommand.Type}";
        if (!string.IsNullOrWhiteSpace(env))
        {
            labelSelector += $",{Constants.LableEnv}={env}";
        }

        var apps = await _kubernetes.BatchV1
            .ListNamespacedJobAsync(
                nsName,
                labelSelector: labelSelector
                );

        if (apps.Items.Count < 1)
        {
            return new List<JobDetails>();
        }

        var appList = apps.Items;
        var result = new List<JobDetails>();
        var allPods = (
                await _kubernetes.CoreV1
                    .ListNamespacedPodAsync(nsName,
                    labelSelector: $"{Constants.LableIamId}={iamId},{Constants.LableAppType}={JobEditCommand.Type}")
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

        return result.OrderBy(x => x.Name);
    }

    public async Task<IEnumerable<JobGrpInfo>> ListGrpAsync(int iamId)
    {
        var label = $"{Constants.LableIamId}={iamId}";

        var jobs = await _kubernetes.BatchV1
            .ListNamespacedJobAsync(
            _iamService.GetNsName(iamId),
            labelSelector: label
            );

        return jobs.Items
            .Select(x => x.Metadata.Labels[Constants.LableAppGrpName])
            .ToHashSet()
            .Select(x => new JobGrpInfo()
            {
                Name = x
            });
    }


    public async Task PublishAsync(
        int iamId,
        AppPublishCommand command
        )
    {
        string dnName = command.AppName;
        var nsName = _iamService.GetNsName(iamId);
        var app = await _kubernetes.BatchV1
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
        await _kubernetes.BatchV1
            .PatchNamespacedJobAsync(
            new V1Patch(patch, V1Patch.PatchType.JsonPatch),
            dnName,
            nsName
            );
    }
}
