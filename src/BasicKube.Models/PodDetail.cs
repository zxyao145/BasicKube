using System.ComponentModel;

namespace BasicKube.Models;

public class PodDetail
{
    /// <summary>
    /// Pod name
    /// </summary>
    [DisplayName("PodName")]
    public string Name { get; set; } = "";

    public string HostIp { get; set; } = "";

    public string PodIp { get; set; } = "";

    public DateTime? StartTime { get; set; }

    public string Status { get; set; } = "";

    public Dictionary<string, ContainerDetail> ContainerDetails { get; set; } =
        new Dictionary<string, ContainerDetail>();

    public string GetStartTimeStr()
    {
        if (StartTime == null)
        {
            return "";
        }

        return StartTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public override bool Equals(object? obj)
    {
        if (obj is PodDetail podDetail)
        {
            return Equals(podDetail);
        }

        return false;
    }

    protected bool Equals(PodDetail other)
    {
        return Name == other.Name
               && HostIp == other.HostIp
               && PodIp == other.PodIp
               && Nullable.Equals(StartTime, other.StartTime)
               && Status == other.Status;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, HostIp, PodIp, StartTime, Status);
    }
}

public class ContainerDetail
{
    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    public string Tag { get; set; } = "";
    public string State { get; set; } = "";
    public bool IsReady { get; set; }

    public int RestartCount { get; set; }
    public int ExitCode { get; set; }

    // cpu usage
    public double Cpu { get; set; }

    // mem usage
    public double Memory { get; set; }
}