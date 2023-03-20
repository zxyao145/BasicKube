namespace BasicKube.Api.Common;

public class K8sUtil
{
    public static string GetEnvByPodName(string podName)
    {
        var segment = podName.Split('-');
        return segment[^3];
    }

    public static string GetEnvByAppName(string podName)
    {
        var segment = podName.Split('-');
        return segment[^1];
    }
}
