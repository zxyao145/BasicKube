
namespace BasicKube.Models;
public class AppDetailsQuery
{
    /// <summary>
    /// DaemonSetName
    /// </summary>
    public string DeployUnitName { get; set; } = "";


    /// <summary>
    /// DaemonSetName
    /// </summary>
    public string Name
    {
        get => DeployUnitName;
        set => DeployUnitName = value;
    }

    public List<PodDetail> PodDetails { get; set; } = new List<PodDetail>();

}
