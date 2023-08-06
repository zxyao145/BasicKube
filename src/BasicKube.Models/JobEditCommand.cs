namespace BasicKube.Models;
#nullable disable

public class JobEditCommand : AppEditCommand
{
    public static string Type => "job";

    public override string TypeName => Type;

    /// <summary>
    /// pod 运行超时时间,优先级高于BackoffLimit
    /// </summary>
    public long? ActiveDeadlineSeconds { get; set; }

    /// <summary>
    /// 失败重试次数
    /// </summary>
    public int? BackoffLimit { get; set; } = 6;

    /// <summary>
    /// Job 完成需要运行多少个 Pod，默认是 1 个。
    /// </summary>
    public int? Completions { get; set; } = 1;

    /// <summary>
    /// 允许并发运行的 Pod 数量，避免过多占用资源。
    /// </summary>
    public int? Parallelism { get; set; } = 1;
}

public class JobInfo
{
    /// <summary>
    /// pod 运行超时时间,优先级高于BackoffLimit
    /// </summary>
    public long? ActiveDeadlineSeconds { get; set; }

    /// <summary>
    /// 失败重试次数
    /// </summary>
    public int? BackoffLimit { get; set; } = 6;

    /// <summary>
    /// Job 完成需要运行多少个 Pod，默认是 1 个。
    /// </summary>
    public int? Completions { get; set; } = 1;

    /// <summary>
    /// 允许并发运行的 Pod 数量，避免过多占用资源。
    /// </summary>
    public int? Parallelism { get; set; } = 1;
}

public class CronJobEditCommand : AppEditCommand
{
    public static string Type => "cron-job";

    public override string TypeName => Type;

    /// <summary>
    /// cron 表达式
    /// </summary>
    public string Schedule { get; set; } = "* * * * *";

    /// <summary>
    /// 成功的 CronJob 执行项数量
    /// k8s 默认为3
    /// </summary>
    public int? SuccessfulJobsHistoryLimit { get; set; } = 10;

    /// <summary>
    /// 失败的 CronJob 执行项数量
    /// k8s 默认为1
    /// </summary>
    public int? FailedJobsHistoryLimit { get; set; } = 5;

    /// <summary>
    /// 延迟作业开始的截止日期
    /// 如果您为 startingDeadlineSeconds 字段指定空值，则 CronJob 永远不会超时。
    /// 这可能会导致同一个 CronJob 同时运行多次。您可以通过指定并发政策来避免这种情况。
    /// </summary>
    public long? StartingDeadlineSeconds { get; set; }

    /// <summary>
    /// 如何处理 CronJob 控制器创建的作业的并发执行。如果您不设置值，则系统默认允许执行多个并发作业。
    /// 
    /// Allow	允许执行并发作业。这是默认设置。;
    /// Forbid 并发作业被禁止，在之前的作业完成或超时之前，新作业无法启动。
    /// Replace 并发作业被禁止，旧作业被取消，取而代之的是新作业。
    /// </summary>
    public string ConcurrencyPolicy { get; set; } = "Allow";


    /// <summary>
    /// 暂停后续执行
    /// suspend 字段设置为 true，将阻止新作业运行，但允许当前执行项完成。
    /// </summary>
    public bool Suspend { get; set; }

    /// <summary>
    /// jobTemplate
    /// </summary>
    public JobInfo JobTemplate { get; set; } = new();
}