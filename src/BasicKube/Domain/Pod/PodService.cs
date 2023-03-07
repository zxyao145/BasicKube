namespace BasicKube.Api.Domain.Pod;

public class PodService : IPodService
{
    private readonly ILogger<PodService> _logger;
    private readonly IKubernetes _kubernetes;
    public PodService(ILogger<PodService> logger, IKubernetes kubernetes)
    {
        _logger = logger;
        _kubernetes = kubernetes;
    }

    public async Task DelAsync(string name, string nsName)
    {
        await _kubernetes.CoreV1.DeleteNamespacedPodAsync(name, nsName);
    }

    public static PodDetail GetPodDetail(V1Pod curPod, bool neeContainerDetails = true)
    {
        var podStatus = curPod.Status;
        var phase = podStatus.Phase;
        if (curPod.Metadata.DeletionTimestamp != null
            && phase == "Running")
        {
            phase = "Terminating";
        }
        // https://github.com/kubernetes-client/python/issues/1004
        //if pod.metadata.deletion_timestamp != None and pod.status.phase in ('Pending, 'Running'):
        //state = 'Terminating'
        //else:
        //state = str(pod.status.phase)

        var podDetail = new PodDetail
        {
            Name = curPod.Metadata.Name,
            HostIp = podStatus.HostIP,
            PodIp = podStatus.PodIP,
            StartTime = podStatus.StartTime,
            // Status = podStatus.Message + ":" + podStatus.Reason,
            Status = phase,
        };

        if (neeContainerDetails)
        {
            var containerStatuss = new Dictionary<string, V1ContainerStatus>();
            if (curPod.Status.ContainerStatuses != null)
            {
                containerStatuss =
                    curPod.Status.ContainerStatuses
                        .ToDictionary(x => x.Name, x => x);
            }

            foreach (var specContainer in curPod.Spec.Containers)
            {
                var imgInfo = specContainer.Image.Split(":");
                var containerDetails = new ContainerDetail()
                {
                    Name = specContainer.Name,
                    Image = imgInfo[0],
                    Tag = imgInfo[1],
                };

                var containerStatus = new V1ContainerStatus()
                {
                    State = new V1ContainerState()
                    {
                        Waiting = new V1ContainerStateWaiting("")
                    },
                    Ready = false,
                    LastState = new V1ContainerState()
                    {
                        Terminated = null
                    }
                };
                if (containerStatuss.ContainsKey(specContainer.Name))
                {
                    containerStatus = containerStatuss[specContainer.Name];
                }

                containerDetails.RestartCount = containerStatus.RestartCount;
                containerDetails.State =
                    containerStatus.State.Waiting != null
                        ? "Waiting"
                        : containerStatus.State.Running != null
                            ? "Running"
                            : "Terminated";

                containerDetails.IsReady = containerStatus.Ready;

                if (containerStatus.LastState.Terminated != null)
                {
                    containerDetails.ExitCode = containerStatus.LastState.Terminated.ExitCode;
                }

                podDetail.ContainerDetails.Add(specContainer.Name, containerDetails);
            }
        }

        return podDetail;
    }
}
