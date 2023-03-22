namespace BasicKube.Models;

public record IngRuleOptions
{
    public int Index { get; set; }

    public string Host { get; set; } = "";

    public string HostType { get; set; } = "http";

    public List<IngRuleValue> RuleValues { get; set; } = new List<IngRuleValue>()
    {
        new IngRuleValue()
    };
}
public record IngRuleValue
{
    public int Index { get; set; }

    public string PathType { get; set; } = "Prefix";

    public string Path { get; set; } = "/";

    public string TargetService { get; set; } = "";

    public int? Port { get; set; }
}
