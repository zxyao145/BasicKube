namespace BasicKube.Models;
#nullable disable

public class SvcEditCommand:IIamModel
{
    public string SvcName => $"{SvcGrpName}-{Env}";

    public int IamId { get; set; }
    public string SvcGrpName { get; set; }
    public string Env { get; set; } = "dev";
    public string Type { get; set; }
    public List<SvcPortInfo> Ports { get; set; } = new List<SvcPortInfo>();
    public string RelationAppName { get; set; } = "";
}

public class SvcType
{
    public const string ClusterIP = "ClusterIP";
    public const string NodePort = "NodePort";
    public const string LoadBalancer = "LoadBalancer";
    public const string ExternalName = "ExternalName";

    public static List<string> SvcTypeList { get; } = new List<string>()
    {
        ClusterIP,
        NodePort
    };
}