namespace BasicKube.Models;
#nullable disable

public class DeployCreateCommand : AppCreateCommand
{
    public static string Type => "deploy";

    public override string TypeName => Type;

    /// <summary>
    /// DeployName
    /// </summary>
    public string DeployName
    {
        get => AppName;
        set => AppName = value;
    }

    /// <summary>
    /// 副本个数
    /// </summary>
    public int Replicas { get; set; } = 1;
}

