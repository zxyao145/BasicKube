

namespace BasicKube.Models;
#nullable disable
public class AutoScaleCreateCommand
{
    public string DeployName { get; set; }

    public int MinReplicas { get; set; }

    public int MaxReplicas { get; set; }

    public List<MetricsInfo> Metrics { get; set; }
}

public class MetricsInfo
{
    public string Type { get; set; }

    public ResourceMetrics Resource { get; set; }
}

/// <summary>
/// CPU, mem AutoScale
/// </summary>
public class ResourceMetrics
{
    public string Name { get; set; }
    public string TargetType { get; } = "Utilization";

    /// <summary>
    /// percentage, min 0, max:100
    /// </summary>
    public int TargetAverageUtilization { get; set; }
}
