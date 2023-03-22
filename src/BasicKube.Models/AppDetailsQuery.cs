namespace BasicKube.Models;

public class AppDetailsQuery
{
    /// <summary>
    /// deploy Name
    /// </summary>
    public string AppName { get; set; } = "";


    /// <summary>
    /// deploy Name
    /// </summary>
    public string Name
    {
        get => AppName;
        set => AppName = value;
    }

    public List<PodDetail> PodDetails { get; set; } = new List<PodDetail>();
}
