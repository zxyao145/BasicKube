using BasicKube.Api.Options;

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
        var env =K8sUtil.GetEnvByAppName(appName);
        return MustGet(env);
    }

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

    public IKubernetes GetOrDefault(string env)
    {
        ArgumentNullException.ThrowIfNull(env);
        if (_cluster.ContainsKey(env))
        {
            return _cluster[env];
        }
        return _cluster["default"];
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
                        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(x.Value);
                        return (IKubernetes)(new Kubernetes(config));
                    }
            );

        serviceConllection.AddSingleton(new KubernetesFactory(kubernetes));

        return serviceConllection;
    }

}
