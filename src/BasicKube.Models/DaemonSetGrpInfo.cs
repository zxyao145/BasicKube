namespace BasicKube.Models;
#nullable disable

public class DaemonSetGrpInfo
{
    public string Name { get; set; }

    public List<int> Ports { get; set; } = new List<int>();
}