using BasicKube.Api.Common;
using BasicKube.Api.Domain.Pod;
using Json.Patch;
using Org.BouncyCastle.Security;
using System.Text.Json;
using System.Xml.Linq;

namespace BasicKube.Api.Domain.App;

public class JobAppService : AppServiceBase<JobDetails, JobCreateCommand>
{
    private readonly IKubernetes _kubernetes;

    private readonly ILogger<DeployAppService> _logger;

    public JobAppService(IamService iamService, IKubernetes kubernetes, ILogger<DeployAppService> logger) : base(iamService)
    {
        _kubernetes = kubernetes;
        _logger = logger;
    }

    #region edit

    public override async Task CreateAsync(int iamId, JobCreateCommand command)
    {
        var nsName = IamService.GetNsName(iamId);
        V1Job job = CreateJob(nsName, command);
        await _kubernetes.BatchV1.CreateNamespacedJobAsync(job, nsName);
    }

    private static V1Job CreateJob(string nsName, JobCreateCommand command)
    {
        var job =  new V1Job
        {
            Metadata = CreateObjectMeta(nsName, command),
            Spec = new V1JobSpec
            {
                ActiveDeadlineSeconds = command.ActiveDeadlineSeconds,
                BackoffLimit = command.BackoffLimit,
                Completions = command.Completions,
                Parallelism = command.Parallelism,
                Selector = GetV1LabelSelector(nsName, command),
                Template = GetPodTemplateSpec(nsName, command)
            }
        };
        job.Validate();
        return job;
    }

    public override async Task UpdateAsync(int iamId, JobCreateCommand command)
    {
        var nsName = IamService.GetNsName(iamId);
        V1Job job = CreateJob(nsName, command);
        await _kubernetes.BatchV1
            .ReplaceNamespacedJobAsync(
                job, job.Metadata.Name,
                job.Metadata.NamespaceProperty
            );
    }



    #endregion

    public override async Task DelAsync(int iamId, string deployUnitName)
    {
        var nsName = IamService.GetNsName(iamId);
        await _kubernetes.BatchV1.DeleteNamespacedJobAsync(deployUnitName, nsName);
    }

    #region Details

    public override async Task<JobCreateCommand?> DetailsAsync(int iamId, string deployUnitName)
    {
        var nsName = IamService.GetNsName(iamId);
        var job = await _kubernetes.BatchV1
            .ReadNamespacedJobAsync(deployUnitName, nsName);

        var cmd = new JobCreateCommand();
        cmd.Containers = GetContainerInfos(job.Spec.Template.Spec.Containers);
        cmd.ActiveDeadlineSeconds = job.Spec.ActiveDeadlineSeconds;
        cmd.BackoffLimit = job.Spec.BackoffLimit;
        cmd.Completions = job.Spec.Completions;
        cmd.Parallelism = job.Spec.Parallelism;
        cmd.RestartPolicy = job.Spec.Template.Spec.RestartPolicy;
        return cmd;
    }

    #endregion

    public override async Task<IEnumerable<JobDetails>> ListAsync
        (int iamId, string appName, string? env = null)
    {
        var nsName = IamService.GetNsName(iamId);
        var labelSelector = $"{Constants.LableIamId}={iamId}," +
            $"{Constants.LableAppName}={appName}," +
            $"{Constants.LableDeployUnitType}={JobCreateCommand.Type}";
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
                    labelSelector: $"{Constants.LableIamId}={iamId},{Constants.LableDeployUnitType}={JobCreateCommand.Type}")
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
        string dnName = command.DeployUnitName;
        var nsName = IamService.GetNsName(iamId);
        // https://github.com/kubernetes-client/csharp/blob/f615b5b4595a35aa6ddc5fed3c396cabdc3f3efa
        // /examples/restart/Program.cs#L25
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
