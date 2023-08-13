
using System.ComponentModel.DataAnnotations;

namespace BasicKube.Models;

public enum PromMetricType
{
    [Display(Name = "cpu")]
    Cpu = 0,

    [Display(Name = "memory")]
    Memory = 1,

    [Display(Name = "restart-count")]
    RestartCount = 2,
}


public class PodPromMetricQuery
{
    public List<string> PodNames { get; set; } = new();

    public List<PromMetricType> Types { get; set; } = new()
    {
        PromMetricType.Cpu,
        PromMetricType.Memory,
    };

    public string ContainerName { get; set; } = "main";
    public long? StartTime { get; set; } = null;
    public long? EndTime { get; set; } = null;
}