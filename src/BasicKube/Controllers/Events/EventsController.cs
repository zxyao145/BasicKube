using BasicKube.Api.Common;

namespace BasicKube.Api.Controllers.Deploy;

public class EventsController : KubeControllerBase
{
    private readonly KubernetesFactory _k8sFactory;

    private readonly ILogger<EventsController> _logger;

    public EventsController(
        ILogger<EventsController> logger,
        KubernetesFactory k8sFactory)
    {
        _logger = logger;
        _k8sFactory = k8sFactory;
    }


    [HttpGet("{podName}")]
    public async Task<ActionResult> GetEvents
        (
         [FromRoute] string podName
        )
    {
        // https://github.com/kubernetes/enhancements/blob/master/keps/sig-instrumentation/383-new-event-api-ga-graduation/README.md#backward-compatibility
        //var ee = await kubernetes.EventsV1.ListNamespacedEventAsync(
        //    namespaceParameter: ns,
        //    fieldSelector: $"regarding.name={podName}"
        //);
        var env = GetEnvByPodName(podName);
        var eventListObj = await _k8sFactory.MustGet(env).CoreV1
            .ListNamespacedEventAsync(
              namespaceParameter: NsName,
              fieldSelector: $"involvedObject.name={podName}"
        // fieldSelector: $"regarding.name={podName}"
        );

        var events = eventListObj.Items
            .Select(x =>
            {
                return new EventInfo()
                {
                    DateTime = x.EventTime
                                ?? x.FirstTimestamp
                                ?? x.LastTimestamp,
                    Type = x.Type,
                    Reason = x.Reason,
                    Source = $"{x.Source.Component},{x.Source.Host}",
                    Message = x.Message,
                };
            })
            .OrderBy(x => x.DateTime);

        return ApiResult.BuildSuccess(events);
    }
}
