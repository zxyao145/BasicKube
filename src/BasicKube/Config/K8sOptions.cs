namespace BasicKube.Api.Config;


public class PromConfig
{
    public bool Enable { get; set; }
    public string BaseAddr { get; set; } = "";
}


public class K8sOptions
{
    public bool EnableMetricServer { get; set; }

    public PromConfig PromConfig { get; set; } = new();

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

