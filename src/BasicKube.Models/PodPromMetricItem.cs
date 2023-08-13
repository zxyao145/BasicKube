using System.Text.Json.Serialization;
using BasicKube.Models.JsonConverters;

namespace BasicKube.Models;

public class PodPromMetricItem
{
    [JsonIgnore]
    public string PodName { get; set; } = "";

    public Dictionary<PromMetricType, PromMetric> Metrics { get; set; } = new();
}

public class PromMetric
{
    public string MetricName { get; set; } = "";

    [JsonConverter(typeof(PromMetricsValueConverter))]
    public List<Tuple<double, string>> Data { get; set; } = new();
}