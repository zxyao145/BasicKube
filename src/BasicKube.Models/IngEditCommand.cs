
using System.Text.Json.Serialization;

namespace BasicKube.Models;

public class IngEditCommand
{
    public static string Type => "ingress";

    [JsonIgnore]
    public string TypeName => Type;

    /// <summary>
    /// 环境
    /// </summary>
    public string Env { get; set; } = "dev";

    /// <summary>
    /// treeid
    /// </summary>
    public int IamId { get; set; }

    public string Description { get; set; } = "";


    /// <summary>
    /// 区域
    /// </summary>
    public string Region { get; set; } = "Asia-Pacific";

    /// <summary>
    /// 机房
    /// </summary>
    public string Room { get; set; } = "CN-1";



    /// <summary>
    /// 所属应用名/微服务服务名
    /// </summary>
    public string IngGrpName { get; set; } = "";

    private string _ingName = "";

    /// <summary>
    /// DeployUnitName
    /// </summary>
    public string IngName
    {
        get => string.IsNullOrWhiteSpace(_ingName) ? $"{IngGrpName}-{Env}" : _ingName;
        set => _ingName = value;
    }

    public string? IngClassName { get; set; } = "nginx";

    public List<IngRuleOptions> Rules { get; set; } = new List<IngRuleOptions>();

    public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>()
    {
        ["nginx.ingress.kubernetes.io/proxy-body-size"] = "20M",
        ["nginx.ingress.kubernetes.io/proxy-connect-timeout"] = "5",
        ["nginx.ingress.kubernetes.io/proxy-send-timeout"] = "20",
        ["nginx.ingress.kubernetes.io/proxy-read-timeout"] = "20",
    };
}
