namespace BasicKube.Models;
#nullable disable

public class JobCreateCommand : AppCreateCommand
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

public class CronJobCreateCommand : AppCreateCommand
{
    public static string Type => "cron-job";

    public override string TypeName => Type;

    /// <summary>
    /// DeployName
    /// </summary>
    public string DeployName
    {
        get => AppName;
        set => AppName = value;
    }

    /// <summary>
    /// 副本个数
    /// </summary>
    public int Replicas { get; set; } = 1;
}