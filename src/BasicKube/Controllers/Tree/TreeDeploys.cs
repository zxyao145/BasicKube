namespace BasicKube.Api.Controllers.Tree;

#nullable disable

public class DeployInfo
{
    public string AppName { get; set; }
    public string DeployName { get; set; }
    public int ReadyReplicas { get; set; }
    public int Replicas { get; set; }
}

public class TreeDeployQuery
{
    public List<DeployInfo> Deploys { get; set; }
}