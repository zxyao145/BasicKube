using KubeClient;

namespace BasicKube.Api.Controllers.Tree;

[ApiController]
[Route("/api/[controller]/[action]")]
public class TreeController : ControllerBase
{
    private readonly KubeApiClient _kubeClient;
    private readonly ILogger<TreeController> _logger;

    public TreeController(KubeApiClient kubeClient, ILogger<TreeController> logger)
    {
        _kubeClient = kubeClient;
        _logger = logger;
    }

    /// <summary>
    /// 获取 treeid 下的所有 deployment
    /// </summary>
    
    /// <param name="ns"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult> GetDeploys(
        int iamId,
        string ns = "default")
    {
        var deploymentList = await _kubeClient.DeploymentsV1()
            .List($"iamId={iamId}", ns);
        if (deploymentList.Items.Count < 1)
        {
            return ApiResult.DataNotExist;
        }

        var deploys = deploymentList
            .Items
            .Select(x => new DeployInfo()
            {
                AppName = x.Metadata.Annotations["appName"] ?? "",
                DeployName = x.Metadata.Name,
                Replicas = x.Status.Replicas ?? 0,
                ReadyReplicas = x.Status.ReadyReplicas ?? 0,
            })
            .OrderBy(x => x.DeployName)
            .ToList();

        var treeDeploys = new TreeDeployQuery()
        {
            Deploys = deploys
        };

        return ApiResult.BuildSuccess(treeDeploys);
    }
}