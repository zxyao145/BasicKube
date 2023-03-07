using k8s;
using KubeClient;

namespace BasicKube.Api.Controllers.App;

[Route("/api/[controller]/[action]/{iamId}/{appName}")]
public abstract class AppControllerBase : KubeControllerBase
{
    public string AppName => (string)HttpContext.Items["AppName"]!;

}