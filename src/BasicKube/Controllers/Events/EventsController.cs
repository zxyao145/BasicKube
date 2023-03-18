﻿namespace BasicKube.Api.Controllers.Deploy;

public class EventsController : KubeControllerBase
{
    private readonly IKubernetes _kubernetes;

    private readonly ILogger<EventsController> _logger;

    public EventsController(
        ILogger<EventsController> logger,
        IKubernetes kubernetes)
    {
        _logger = logger;
        _kubernetes = kubernetes;
    }


    [HttpGet("{podName}")]
    public async Task<ActionResult> GetEvents
        (
         [FromRoute] string podName
        )
    {
        // https://github.com/kubernetes/enhancements/blob/master/keps/sig-instrumentation/383-new-event-api-ga-graduation/README.md#backward-compatibility
        //var ee = await _kubernetes.EventsV1.ListNamespacedEventAsync(
        //    namespaceParameter: ns,
        //    fieldSelector: $"regarding.name={podName}"
        //);

        var eventListObj = await _kubernetes.CoreV1
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
