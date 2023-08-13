
namespace BasicKube.Models;
public class PodMetricsItem
{
    /// <summary>
    /// key: container name
    /// </summary>
    public Dictionary<string, ContainerMetricsItem> ContainerMetrics { get; set; } = new ();
}
public class ContainerMetricsItem
{

    public string Cpu { get; set; } = "";

    public string Memory { get; set; } = "";
}

