using BasicKube.Api.Config;
using k8s.KubeConfigModels;

namespace BasicKube.Api.Common;

public class KubernetesFactory
{
    private Dictionary<string, IKubernetes> _cluster;

    public KubernetesFactory(Dictionary<string, IKubernetes> k8sCluster)
    {
        _cluster = k8sCluster;
    }

    public IKubernetes MustGetByPodName(string podName)
    {
        ArgumentNullException.ThrowIfNull(podName);
        var env = K8sUtil.GetEnvByPodName(podName);
        return MustGet(env);
    }

    public IKubernetes MustGetByAppName(string appName)
    {
        ArgumentNullException.ThrowIfNull(appName);
        var env = K8sUtil.GetEnvByAppName(appName);
        return MustGet(env);
    }

    /// <summary>
    /// get IKubernetes of env or default config
    /// </summary>
    /// <param name="env"></param>
    /// <returns></returns>
    public IKubernetes MustGet(string env)
    {
        ArgumentNullException.ThrowIfNull(env);
        return _cluster[env];
    }

    public IKubernetes? Get(string env)
    {
        ArgumentNullException.ThrowIfNull(env);
        if (_cluster.ContainsKey(env))
        {
            return _cluster[env];
        }
        return null;
    }

    public Dictionary<string, IKubernetes> All => _cluster;
}

public static class K8sServiceExt
{
    public static IServiceCollection AddK8sService(
        this IServiceCollection serviceConllection,
        IConfiguration configuration
        )
    {
        var k8sOptions = configuration.GetSection("K8s")
            .Get<K8sOptions>();
        serviceConllection.Configure<K8sOptions>(configuration.GetSection("K8s"));

        //var kubernetesClientConfig = KubernetesClientConfiguration.BuildDefaultConfig();
        //serviceConllection.AddSingleton<IKubernetes>(new Kubernetes(kubernetesClientConfig));

        var kubernetes = k8sOptions.ClusterConfig
            .ToDictionary(
                x => x.Key,
                x =>
                    {
                        // 外部配置k8s访问信息
                        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(x.Value);
                        return (IKubernetes)(new Kubernetes(config));

                        // 集群内配置k8s访问信息
                        //var config = KubernetesClientConfiguration.InClusterConfig();
                        //return (IKubernetes)(new Kubernetes(config));
                    }
            );


        serviceConllection.AddSingleton(new KubernetesFactory(kubernetes));

        return serviceConllection;
    }
}
