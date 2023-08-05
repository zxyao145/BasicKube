using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Xml.Linq;

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

        public int Succeeded { get; set; }

        public int Failed { get; set; }

        public int Ready { get; set; }
    }


    public class CronJobHistoryInfo
    {
        public string JobName { get; set; } = "";

        public DateTime? StartTime { get; set; }

        public int Succeeded { get; set; }

        public int Failed { get; set; }

        public int Ready { get; set; }

        public bool? Suspend { get; set; }

        public int? Completions { get; set; }

        public string GetStartTimeStr()
        {
            if (StartTime == null)
            {
                return "";
            }

            return StartTime.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }

        public string GetState()
        {
            if (Succeeded == 0 && Failed > 0)
            {
                return "Failed";
            }

            var suspend = Suspend ?? true;
            if (suspend)
            {
                return "Suspended";
            }
            return "Active";
        }


        public override bool Equals(object? obj)
        {
            if (obj is CronJobHistoryInfo other)
            {
                return Equals(other);
            }

            return false;
        }

        protected bool Equals(CronJobHistoryInfo other)
        {
            return JobName == other.JobName
                   && Nullable.Equals(StartTime, other.StartTime)
                   && Succeeded == other.Succeeded
                   && Failed == other.Failed
                   && Ready == other.Ready
                   && Suspend == other.Suspend
                   && Completions == other.Completions;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(JobName, StartTime, Succeeded, Failed, Ready, Suspend, Completions);
        }
    }


    public class CronJobDetails : AppDetailsQuery
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

        public bool Suspend { get; set; }

        public List<CronJobHistoryInfo> FailedHistory { get; set; } = new();

        public List<CronJobHistoryInfo> SuccessHistory { get; set; } = new();

        public override bool Equals(object? obj)
        {
            if (obj is CronJobDetails other)
            {
                return Equals(other);
            }

            return false;

        }

        protected bool Equals(CronJobDetails other)
        {
            return ActiveDeadlineSeconds == other.ActiveDeadlineSeconds
                   && BackoffLimit == other.BackoffLimit
                   && Completions == other.Completions
                   && Parallelism == other.Parallelism
                   && RestartPolicy == other.RestartPolicy
                   && Suspend == other.Suspend;
            //&& FailedHistory.Equals(other.FailedHistory) 
            //&& SuccessHistory.Equals(other.SuccessHistory);
        }

        public override int GetHashCode()
        {
            //return HashCode.Combine(ActiveDeadlineSeconds, BackoffLimit, Completions, Parallelism, RestartPolicy, Suspend, FailedHistory, SuccessHistory);
            return HashCode.Combine(ActiveDeadlineSeconds, BackoffLimit, Completions, Parallelism, RestartPolicy, Suspend);
        }
    }
}