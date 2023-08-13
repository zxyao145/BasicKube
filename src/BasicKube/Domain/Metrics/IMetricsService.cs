using k8s;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Linq;

namespace BasicKube.Api.Domain.Metrics;

public interface IMetricsService
{
    Task<Dictionary<string, PodMetricsItem>> GetPodMetricsList
        (int iamId, string grpName, string envName);

    Task<Dictionary<string, PodPromMetricItem>> GetPromPodMetricsList(
        int iamId,
        PodPromMetricQuery query,
        string envName
    );
}
