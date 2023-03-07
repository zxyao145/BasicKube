namespace BasicKube.Models;

#nullable disable

public class SvcInfo
{
    public int IamId { get; set; }

    public string Name { get; set; }

    public List<SvcPortInfo> Ports { get; set; } = new List<SvcPortInfo>();

    public List<Selector> Selectors { get; set; } = new List<Selector>();

    public string Type { get; set; }
    public string ClusterIp { get; set; }
    public DateTime? CreateTime { get; set; }
    public string Status { get; set; }

    public IEnumerable<PodDetail> PodDetails { get; set; }

    public string GetCreateTimeStr()
    {
        if (CreateTime == null)
        {
            return "";
        }

        return CreateTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public override bool Equals(object obj)
    {
        if (obj is SvcInfo svcInfo)
        {
            return Equals(svcInfo);
        }

        return false;
    }

    protected bool Equals(SvcInfo other)
    {
        return IamId == other.IamId
               && Name == other.Name
               && ClusterIp == other.ClusterIp
               && Type == other.Type
               && Nullable.Equals(CreateTime, other.CreateTime);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IamId, Name, IamId, Type, CreateTime);
    }
}

public record SvcPortInfo : PortInfo
{
    /// <summary>
    ///
    /// </summary>
    public int TargetPort { get; set; }
    /// <summary>
    ///
    /// </summary>
    public int? NodePort { get; set; }
}

public record Selector
{
    public string Key { get; set; }
    public string Value { get; set; }
}