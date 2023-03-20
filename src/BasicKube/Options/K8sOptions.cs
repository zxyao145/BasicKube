namespace BasicKube.Api.Options;


public class K8sOptions
{
    /// <summary>
    /// key：IAMId
    /// value：namespace
    /// </summary>
    public Dictionary<string, string> NameSpaceMap { get; set; } = new();

    /// <summary>
    /// key：environment
    /// value：k8s config file path
    /// </summary>
    public Dictionary<string, string> ClusterConfig { get; set; } = new();

}

