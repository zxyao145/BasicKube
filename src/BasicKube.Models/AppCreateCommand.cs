
using System.Text.Json.Serialization;

namespace BasicKube.Models;

public abstract class AppEditCommand: IIamModel
{
    [JsonIgnore]
    public abstract string TypeName { get; }

    /// <summary>
    /// 所属应用名/微服务服务名
    /// </summary>
    public string GrpName { get; set; } = "";

    /// <summary>
    /// 环境
    /// </summary>
    public string Env { get; set; } = "dev";

    private string _appName = "";

    /// <summary>
    /// AppName
    /// </summary>
    public string AppName
    {
        get => string.IsNullOrWhiteSpace(_appName) ? $"{GrpName}-{Env}" : _appName;
        set => _appName = value;
    }


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
    /// Restart policy for all containers within the pod. 
    /// One of Always, OnFailure, Never. Default to Always. More info:
    /// https://kubernetes.io/docs/concepts/workloads/pods/pod-lifecycle/#restart-policy
    /// 
    /// </summary>
    public string RestartPolicy { get; set; } = "Always";

    public List<ContainerInfo> Containers { get; set; } = new List<ContainerInfo>();
}
