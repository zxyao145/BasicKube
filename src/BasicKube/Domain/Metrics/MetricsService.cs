using System.Net;
using BasicKube.Api.Config;
using BasicKube.Api.Domain.Prom;
using Microsoft.Extensions.Options;

namespace BasicKube.Api.Domain.Metrics;

[Service<IMetricsService>]
public class MetricsService : IMetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private readonly KubernetesFactory _kubernetes;
    private readonly IamService _iamService;
    private readonly K8sOptions _k8sOptions;
    private readonly PromHttpClient _promHttpClient;

    public MetricsService(
        ILogger<MetricsService> logger,
        KubernetesFactory kubernetes,
        IamService iamService,
        IOptions<K8sOptions> options,
        PromHttpClient promHttpClient)
    {
        _logger = logger;
        _kubernetes = kubernetes;
        _iamService = iamService;
        _promHttpClient = promHttpClient;
        _k8sOptions = options.Value;
    }


    public async Task<Dictionary<string, PodMetricsItem>>
        GetPodMetricsList(int iamId, string grpName, string envName)
    {
        if (!_k8sOptions.EnableMetricServer)
        {
            return new Dictionary<string, PodMetricsItem>();
        }

        var nsName = _iamService.GetNsName(iamId);
        var podLabelSelector = 
            $"{K8sLabelsConstants.LabelIamId}={iamId}" +
            $",{K8sLabelsConstants.LabelEnv}={envName}" +
            $",{K8sLabelsConstants.LabelApp}={grpName}-{envName}";

        var podMetricsList = await InternalGetPodMetricsList(podLabelSelector, nsName, envName);
        var result = new Dictionary<string, PodMetricsItem>();
        if (podMetricsList == null)
        {
            return result;
        }

        foreach (var podMetrics in podMetricsList.Items)
        {
            var podMetricsItem = new PodMetricsItem();
            var containers = podMetrics.Containers
                .ToDictionary(
                    x => x.Name,
                    x => new ContainerMetricsItem
                    {
                        Cpu = x.Usage["cpu"]?.Value ?? "0",
                        Memory = x.Usage["memory"]?.Value ?? "0",
                    }
                );
            podMetricsItem.ContainerMetrics = containers;
            result.Add(podMetrics.Name(), podMetricsItem);
        }
        return result;
    }

    private async Task<PodMetricsList?> InternalGetPodMetricsList
        (string podLabelSelector, string nsName, string envName)
    {
        //var urlEncode = UrlEncoder.Default.Encode(podLabelSelector);
        //var path = $"/apis/metrics.k8s.io/v1beta1/namespaces/{nsName}/pods?labelSelector={urlEncode}";
        //var kubernetes = (Kubernetes)_kubernetes.MustGet(envName);

        //var rspMsg = await kubernetes.HttpClient.GetAsync(path);

        //if (rspMsg.IsSuccessStatusCode)
        //{
        //    var rspContent = await rspMsg.Content.ReadAsStringAsync();
        //    var result = JsonSerializer
        //        .Deserialize<PodMetricsList>(rspContent);

        //    return result;
        //}

        //return null;

        var kubernetes = _kubernetes.MustGet(envName);

        var p = await kubernetes
            .GetKubernetesPodsMetricsByNamespaceAsync("default");
        var customObject = await kubernetes
            .CustomObjects
            .ListNamespacedCustomObjectAsync
            ("metrics.k8s.io",
                "v1beta1",
                nsName,
                "pods",
                labelSelector: podLabelSelector
            )
            .ConfigureAwait(false);

        var resp = await kubernetes
            .CustomObjects
            .ListNamespacedCustomObjectWithHttpMessagesAsync(
                "metrics.k8s.io",
                "v1beta1",
                nsName,
                "pods",
                labelSelector: podLabelSelector)
            .ConfigureAwait(false);
        var result = KubernetesJson.Deserialize<PodMetricsList>(resp.Body.ToString());
        return result;
        //kubernetes.GetKubernetesPodsMetricsByNamespaceAsync()
        //var customObject = (JsonElement)await kubernetes
        //    .CustomObjects
        //    .GetClusterCustomObjectAsync(
        //        "metrics.k8s.io", 
        //        "v1beta1", 
        //        "pods", 
        //        string.Empty)
        //    .ConfigureAwait(false);
        //customObject.Deserialize<PodMetricsList>();
    }


    public async Task<Dictionary<string, PodPromMetricItem>>
        GetPromPodMetricsList(
            int iamId,
            PodPromMetricQuery query,
            string envName
            )
    {
        var res = new Dictionary<string, PodPromMetricItem>();

        if (!_k8sOptions.PromConfig.Enable)
        {
            return res;
        }
        var nsName = _iamService.GetNsName(iamId);

        query.EndTime ??= DateTimeOffset.Now.ToUnixTimeSeconds();
        query.StartTime ??= DateTimeOffset.Now.AddHours(-1).ToUnixTimeSeconds();

        var tasks = new List<Task<PodPromMetricItem>>();
        foreach (var podName in query.PodNames)
        {
            var task = Task.Run(async () =>
                await QueryMetricsFromProm(query, podName, nsName)
                );
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        foreach (var task in tasks)
        {
            var taskResult = await task;
            res[taskResult.PodName] = taskResult;
        }
        return res;
    }

    private async Task<PodPromMetricItem> QueryMetricsFromProm(PodPromMetricQuery query, string podName, string nsName)
    {
        var item = new PodPromMetricItem
        {
            PodName = podName
        };

        foreach (var promMetricType in query.Types)
        {
            switch (promMetricType)
            {
                case PromMetricType.Cpu:
                {
                    var cpu = await QueryCpuUsageMem(
                        podName,
                        nsName,
                        query.ContainerName,
                        query.StartTime,
                        query.EndTime);

                    item.Metrics[promMetricType] = new PromMetric
                    {
                        MetricName = promMetricType.GetDisplayName(),
                        Data = cpu
                    };
                    break;
                }
                case PromMetricType.Memory:
                {
                    var mem = await QueryMemUsage(
                        podName,
                        nsName,
                        query.ContainerName,
                        query.StartTime,
                        query.EndTime);
                    item.Metrics[promMetricType] = new PromMetric
                    {
                        MetricName = promMetricType.GetDisplayName(),
                        Data = mem
                    };
                    break;
                }
                default:
                    break;
            }
        }

        return item;
    }

    private async Task<List<Tuple<double, string>>> QueryCpuUsageMem(
        string podName,
        string nsName = "default",
        string containerName = "main",
        long? startTime = null,
        long? endTime = null,
        string step = "15s"
    )
    {
        var promQl = $"sum(rate(container_cpu_usage_seconds_total{{pod=\"{podName}\",namespace=\"{nsName}\",container=\"{containerName}\"}}[1m]))";
        endTime ??= DateTimeOffset.Now.ToUnixTimeSeconds();
        startTime ??= DateTimeOffset.Now.AddHours(-1).ToUnixTimeSeconds();

        var queryPath =
            $"/api/v1/query_range?query={promQl}" +
            $"&start={startTime.Value}" +
            $"&end={endTime.Value}" +
            $"&step={step}";

        var res = await _promHttpClient.Query(queryPath);
        List<Tuple<double, string>> result = new();
        if (res != null)
        {
            result = res
                .Data.Result[0]
                .Values
                .Select(x => new Tuple<double, string>((double)x[0], (string)x[1]))
                .ToList();
        }

        return result;
    }

    private async Task<List<Tuple<double, string>>> QueryMemUsage(
        string podName,
        string nsName = "default",
        string containerName = "main",
        long? startTime = null,
        long? endTime = null,
        string step = "15s"
    )
    {
        var promQl = $"container_memory_rss{{pod=\"{podName}\",namespace=\"{nsName}\",container=\"{containerName}\"}}";
        endTime ??= DateTimeOffset.Now.ToUnixTimeSeconds();
        startTime ??= DateTimeOffset.Now.AddHours(-1).ToUnixTimeSeconds();

        var queryPath =
            $"/api/v1/query_range?query={promQl}" +
            $"&start={startTime.Value}" +
            $"&end={endTime.Value}" +
            $"&step={step}";

        var res = await _promHttpClient.Query(queryPath);

        List<Tuple<double, string>> result = new();
        if (res != null)
        {
            result = res
                .Data.Result[0]
                .Values
                .Select(x => new Tuple<double, string>((double)x[0], (string)x[1]))
                .ToList();
        }

        return result;
    }

}

public static class K8sExt
{
    public static async Task<Dictionary<string, PodMetricsItem>>
        GetPodMetricsList(
            this IKubernetes kubernetes,
            K8sOptions k8sOptions,
            string podLabelSelector,
            string nsName)
    {
        if (!k8sOptions.EnableMetricServer)
        {
            return new Dictionary<string, PodMetricsItem>();
        }

        var podMetricsList = await InternalGetPodMetricsList(
            kubernetes,
            podLabelSelector, nsName);
        var result = new Dictionary<string, PodMetricsItem>();
        if (podMetricsList == null)
        {
            return result;
        }

        foreach (var podMetrics in podMetricsList.Items)
        {
            var podMetricsItem = new PodMetricsItem();
            var containers = podMetrics.Containers
                .ToDictionary(
                    x => x.Name,
                    x => new ContainerMetricsItem
                    {
                        Cpu = x.Usage["cpu"]?.Value ?? "0",
                        Memory = x.Usage["memory"]?.Value ?? "0",
                    }
                );
            podMetricsItem.ContainerMetrics = containers;
            result.Add(podMetrics.Name(), podMetricsItem);
        }
        return result;
    }

    private static async Task<PodMetricsList?> InternalGetPodMetricsList
      (IKubernetes kubernetes,
          string podLabelSelector,
          string nsName)
    {
        //var p = await kubernetes
        //    .GetKubernetesPodsMetricsByNamespaceAsync("default");
        //var customObject = await kubernetes
        //    .CustomObjects
        //    .ListNamespacedCustomObjectAsync
        //    ("metrics.k8s.io",
        //        "v1beta1",
        //        nsName,
        //        "pods",
        //        labelSelector: podLabelSelector
        //    )
        //    .ConfigureAwait(false);

        var resp = await kubernetes
            .CustomObjects
            .ListNamespacedCustomObjectWithHttpMessagesAsync(
                "metrics.k8s.io",
                "v1beta1",
                nsName,
                "pods",
                labelSelector: podLabelSelector)
            .ConfigureAwait(false);

        if (resp.Response.StatusCode != HttpStatusCode.OK)
        {
            return new PodMetricsList();
        }

        var result = KubernetesJson.Deserialize<PodMetricsList>(resp.Body.ToString());
        return result;
        //kubernetes.GetKubernetesPodsMetricsByNamespaceAsync()
        //var customObject = (JsonElement)await kubernetes
        //    .CustomObjects
        //    .GetClusterCustomObjectAsync(
        //        "metrics.k8s.io", 
        //        "v1beta1", 
        //        "pods", 
        //        string.Empty)
        //    .ConfigureAwait(false);
        //customObject.Deserialize<PodMetricsList>();
    }

}