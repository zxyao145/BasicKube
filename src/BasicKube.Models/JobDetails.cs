using System.ComponentModel.DataAnnotations;

namespace BasicKube.Models
{
    public class JobDetails : AppDetailsQuery
    {
        /// <summary>
        /// pod 运行超时时间,优先级高于BackoffLimit
        /// </summary>
        public long? ActiveDeadlineSeconds { get; set; }

        /// <summary>
        /// 失败重试次数
        /// </summary>
        public int? BackoffLimit { get; set; }

        /// <summary>
        /// Job 完成需要运行多少个 Pod，默认是 1 个。
        /// </summary>
        public int? Completions { get; set; }

        /// <summary>
        /// 允许并发运行的 Pod 数量，避免过多占用资源。
        /// </summary>
        public int? Parallelism { get; set; }

        public string RestartPolicy { get; set; } = "Always";

    }

    public class CronJobDetails : AppDetailsQuery
    {
        [MinLength(0)]
        public int ReadyReplicas { get; set; }

        [MinLength(0)]
        public int Replicas { get; set; }
    }
}