using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace BasicKube.Api.Domain.Prom;

public class PromQueryResultModel
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";
   
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("data")]
    public Data Data { get; set; } = new Data();
}

public class Data
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("resultType")]
    public string ResultType { get; set; } = "";

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("result")]
    public List<ResultItem> Result { get; set; } = new();
}


public class ResultItem
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("metric")]
    public Metric MetricMeta { get; set; } = new();

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("values")]
    public List<JsonArray> Values { get; set; } = new();
}


public class Metric
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("__name__")]
    public string MetricName { get; set; } = "";

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("container")]
    public string Container { get; set; } = "";

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = "";

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("image")]
    public string Image { get; set; } = "";

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("instance")] 
    public string Instance { get; set; } = "";

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("job")]
    public string Job { get; set; } = "";

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("metrics_path")]
    public string MetricsPath { get; set; } = "";

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = "";

    /// <summary>
    /// 
    /// </summary>

    [JsonPropertyName("node")]
    public string Node { get; set; } = "";
    /// <summary>
    /// 
    /// </summary>
    
    [JsonPropertyName("pod")]
    public string Pod { get; set; } = "";
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("service")]
    public string Service { get; set; } = "";
}
