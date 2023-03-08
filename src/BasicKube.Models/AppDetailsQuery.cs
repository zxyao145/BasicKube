
namespace BasicKube.Models;
public class AppDetailsQuery
{
    /// <summary>
    /// DaemonSetName
    /// </summary>
    public string AppName { get; set; } = "";


    /// <summary>
    /// DaemonSetName
    /// </summary>
    public string Name
    {
        get => AppName;
        set => AppName = value;
    }

    public List<PodDetail> PodDetails { get; set; } = new List<PodDetail>();

}
