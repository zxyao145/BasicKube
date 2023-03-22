namespace BasicKube.Api.Controllers.Core;

[ApiController]
[Route($"/api/[controller]/[action]/{{{RouteConstants.IamId}}}")]
public abstract class KubeControllerBase : ControllerBase
{
    public string NsName => (string)HttpContext.Items[RouteConstants.NsName]!;

    public int IamId => (int)HttpContext.Items[RouteConstants.IamId]!;

    public string? EnvName
    {
        get
        {
            object? obj = HttpContext.Items[RouteConstants.Env];
            if (obj == null)
            {
                return null;
            }
            return (string)obj;
        }
    }

    public static string GetEnvByPodName(string podName)
    {
        return K8sUtil.GetEnvByPodName(podName);
    }

    public static string GetEnvByAppName(string appName)
    {
        return K8sUtil.GetEnvByAppName(appName);
    }
}