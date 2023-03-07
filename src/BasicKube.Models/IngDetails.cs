
namespace BasicKube.Models;

public class IngDetails
{
    public string Name { get; set; } = "";

    public DateTime? CreateTime { get; set; }

    public DateTime? UpdateTime { get; set; }

    public string? IngClassName { get; set; } = "";

    public IEnumerable<string> LbIps { get; set; } = new List<string>();

    public IEnumerable<IngRuleOptions> Rules { get; set; } = new List<IngRuleOptions>();

    public string GetCreateTimeStr()
    {
        if (CreateTime == null)
        {
            return "";
        }

        return CreateTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public override bool Equals(object? obj)
    {
        if (obj is IngDetails details)
        {
            return Equals(details);
        }

        return false;
    }

    protected bool Equals(IngDetails details)
    {
        return Name == details.Name
               && IngClassName == details.IngClassName
               && CreateTime == details.CreateTime
               && UpdateTime == details.UpdateTime
               && string.Join(",", LbIps) == string.Join(",", details.LbIps);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, IngClassName, string.Join(",", LbIps), CreateTime, UpdateTime);
    }
}
