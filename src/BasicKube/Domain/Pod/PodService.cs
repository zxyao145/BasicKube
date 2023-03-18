using BasicKube.Api.Common;
using BasicKube.Api.Exceptions;
using Org.BouncyCastle.Security;

namespace BasicKube.Api.Domain.Pod;

[Service]
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

    #region GetContainerInfos

    public static List<ContainerInfo> GetContainerInfos(IList<V1Container> containers)
    {
        var containerInfos = new List<ContainerInfo>();

        var containerIndex = 0;
        foreach (var item in containers)
        {
            var img = item.Image;
            var imgInfo = img.Split(":");
            var containerInfo = new ContainerInfo()
            {
                Index = containerIndex++,
                Name = item.Name,
                Image = imgInfo[0],
                Tag = imgInfo[1],
                StartCmd = item.Command is { Count: > 0 } ? string.Join(" ", item.Command) : "",
                AfterStart = MergeCommand(item.Lifecycle?.PostStart),
                BeforeStop = MergeCommand(item.Lifecycle?.PreStop),
                // Cpu =
                // Memory =
                ReadinessProbe = GetProbe(item.ReadinessProbe),
                LivenessProbe = GetProbe(item.LivenessProbe),
                EnvVars = GetEnvVarInfos(item.Env)
            };

            if (item.Ports != null)
            {
                var ports = item.Ports.Select((x, index) => new PortInfo()
                {
                    Index = index,
                    Port = x.ContainerPort,
                    Protocol = x.Protocol,
                });

                containerInfo.Ports = ports.ToList();
            }

            containerInfos.Add(containerInfo);
        }
        return containerInfos;
    }

    private static List<EnvVarInfo> GetEnvVarInfos(IList<V1EnvVar> envVars)
    {
        var envVarInfos = new List<EnvVarInfo>();
        var index = 0;
        foreach (var item in envVars)
        {
            if (item.ValueFrom == null)
            {
                envVarInfos.Add(new EnvVarInfo()
                {
                    Key = item.Name,
                    Value = item.Value,
                    Index = index++
                });
            }
        }

        return envVarInfos;
    }

    private static Probe? GetProbe(V1Probe? probeV1)
    {
        if (probeV1 == null)
        {
            return null;
        }
        var probeInfo = new Probe();
        if (probeV1.HttpGet != null)
        {
            probeInfo.Port = Convert.ToInt32(probeV1.HttpGet.Port.Value);
            probeInfo.Path = probeV1.HttpGet.Path;
            foreach (var header in probeV1.HttpGet.HttpHeaders)
            {
                probeInfo.Header.Add(header.Name, header.Value);
            }
        }
        else if (probeV1.TcpSocket != null)
        {
            probeInfo.Port = Convert.ToInt32(probeV1.TcpSocket.Port.Value);
        }
        else if (probeV1.Exec != null)
        {
            probeInfo.Cmd = string.Join(" ", probeV1.Exec.Command);
        }
        else
        {
            return null;
        }

        return probeInfo;
    }

    private static string MergeCommand(V1LifecycleHandler? handlerV1)
    {
        if (handlerV1 == null)
        {
            return "";
        }

        return string.Join(" ", handlerV1.Exec.Command);
    }

    #endregion

    #region PodTemplateSpec

    public static V1PodTemplateSpec GetPodTemplateSpec
        (string nsName, AppEditCommand command, string type = "")
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            type = command.TypeName;
        }
        var podTemp = new V1PodTemplateSpec()
        {
            Metadata = new V1ObjectMeta
            {
                Name = command.AppName,
                NamespaceProperty = nsName,
                Annotations = new Dictionary<string, string>
                {
                    [K8sLabelsConstants.LabelRegion] = command.Region,
                    [K8sLabelsConstants.LabelRoom] = command.Room,
                },
                Labels = new Dictionary<string, string>
                {
                    [K8sLabelsConstants.LabelIamId] = command.IamId + "",
                    [K8sLabelsConstants.LabelEnv] = command.Env,

                    // pod 独有的
                    [K8sLabelsConstants.LabelApp] = command.AppName,
                }
            },
            Spec = new V1PodSpec()
        };

        if (!string.IsNullOrWhiteSpace(type))
        {
            podTemp.Metadata.Labels.Add(K8sLabelsConstants.LabelAppType, type);
        }

        podTemp.Spec.RestartPolicy = command.RestartPolicy;
        podTemp.Spec.Containers = CreateContainers(nsName, command);
        return podTemp;
    }

    private static IList<V1Container> CreateContainers
        (string nsName, AppEditCommand command)
    {
        var containers = new List<V1Container>();
        foreach (var item in command.Containers)
        {
            var container = new V1Container()
            {
                Image = $"{item.Image}:{item.Tag}",
                Name = item.Name,
                Env = new List<V1EnvVar>()
                    {
                        new V1EnvVar()
                        {
                            Name = "POD_NAME",
                            ValueFrom = new V1EnvVarSource()
                            {
                                FieldRef = new V1ObjectFieldSelector()
                                {
                                    FieldPath = "metadata.name",
                                    ApiVersion = "v1"
                                }
                            }
                        },
                        new V1EnvVar()
                        {
                            Name = "POD_IP",
                            ValueFrom = new V1EnvVarSource()
                            {
                                FieldRef = new V1ObjectFieldSelector()
                                {
                                    FieldPath = "status.podIP",
                                    ApiVersion = "v1"
                                }
                            }
                        },
                        new V1EnvVar()
                        {
                            Name = "NODE_NAME",
                            ValueFrom = new V1EnvVarSource()
                            {
                                FieldRef = new V1ObjectFieldSelector()
                                {
                                    FieldPath = "spec.nodeName",
                                    ApiVersion = "v1"
                                }
                            }
                        },
                        new V1EnvVar()
                        {
                            Name = "NODE_IP",
                            ValueFrom = new V1EnvVarSource()
                            {
                                FieldRef = new V1ObjectFieldSelector()
                                {
                                    FieldPath = "status.hostIP",
                                    ApiVersion = "v1"
                                }
                            }
                        },
                        new V1EnvVar()
                        {
                            Name = "IAM_TREE_ID",
                            ValueFrom = new V1EnvVarSource()
                            {
                                FieldRef = new V1ObjectFieldSelector()
                                {
                                    FieldPath = "metadata.labels['iamId']",
                                    ApiVersion = "v1"
                                }
                            }
                        },
                        new V1EnvVar()
                        {
                            Name = "REGION",
                            ValueFrom = new V1EnvVarSource()
                            {
                                FieldRef = new V1ObjectFieldSelector()
                                {
                                    FieldPath = "metadata.annotations['region']",
                                    ApiVersion = "v1"
                                }
                            }
                        }
                    }
            };
            if (item.Ports is { Count: > 0 })
            {
                container.Ports = new List<V1ContainerPort>();
                foreach (var itemPort in item.Ports)
                {
                    var portInfo = new V1ContainerPort()
                    {
                        ContainerPort = itemPort.Port,
                        Protocol = itemPort.Protocol,
                    };
                    container.Ports.Add(portInfo);
                }
            }

            #region Resources

            if (item.Cpu < 0.1)
            {
                item.Cpu = 0.1;
            }

            if (item.Memory < 0.01)
            {
                item.Memory = 0.01;
            }
#if DEBUG
            item.Cpu = 0.1;
            if (item.Memory > 0.4)
            {
                item.Memory = 0.4;
            }
#endif
            container.Resources ??= new V1ResourceRequirements()
            {
                Requests = new Dictionary<string, ResourceQuantity>(),
                Limits = new Dictionary<string, ResourceQuantity>(),
            };
            container.Resources.Requests["cpu"] = new ResourceQuantity($"{item.Cpu: 0.0}");
            container.Resources.Limits["cpu"] = new ResourceQuantity($"{item.Cpu * 1.25: 0.0}");

            container.Resources.Requests["memory"] = new ResourceQuantity($"{item.Memory: 0.0}Gi");
            container.Resources.Limits["memory"] = new ResourceQuantity($"{item.Memory * 1.25: 0.0}Gi");
#if DEBUG
            container.Resources = null;
#endif

            #endregion Resources

            if (item.EnvVars is { Count: > 0 })
            {
                foreach (var itemEnvVar in item.EnvVars)
                {
                    container.Env.Add(new V1EnvVar()
                    {
                        Name = itemEnvVar.Key,
                        Value = itemEnvVar.Value
                    });
                }
            }

            #region Lifecycle

            if (!string.IsNullOrWhiteSpace(item.StartCmd))
            {
                container.Command = HandlerCmd(item.StartCmd);
            }

            if (!string.IsNullOrWhiteSpace(item.AfterStart))
            {
                container.Lifecycle = new V1Lifecycle();
                container.Lifecycle.PostStart = new V1LifecycleHandler()
                {
                    Exec = new V1ExecAction(HandlerCmd(item.AfterStart))
                };
            }

            if (!string.IsNullOrWhiteSpace(item.BeforeStop))
            {
                container.Lifecycle ??= new V1Lifecycle();
                container.Lifecycle.PreStop = new V1LifecycleHandler()
                {
                    Exec = new V1ExecAction(HandlerCmd(item.BeforeStop))
                };
            }

            #endregion Lifecycle

            if (item.ReadinessProbe != null)
            {
                container.ReadinessProbe = CreateProbe(item.ReadinessProbe);
            }

            if (item.LivenessProbe != null)
            {
                container.LivenessProbe = CreateProbe(item.LivenessProbe);
            }

            containers.Add(container);
        }

        return containers;
    }


    private static V1Probe CreateProbe(Probe probe)
    {
        var probeV1 = new V1Probe()
        {
            PeriodSeconds = probe.PeriodSeconds,
            TimeoutSeconds = probe.TimeoutSeconds,
            FailureThreshold = probe.FailureThreshold,
            InitialDelaySeconds = probe.InitialDelaySeconds,
        };

        if (probe.Type == "http")
        {
            IList<V1HTTPHeader>? httpHeaders = null;
            if (probe.Header is { Count: > 0 })
            {
                httpHeaders = probe.Header
                    .Select(x => new V1HTTPHeader(x.Key, x.Value))
                    .ToList();
            }

            probeV1.HttpGet = new V1HTTPGetAction(
                probe.Port,
                "127.0.0.1",
                httpHeaders,
                probe.Path,
                "HTTP"
                );
        }
        else if (probe.Type == "tcp")
        {
            probeV1.TcpSocket = new V1TCPSocketAction()
            {
                Host = "127.0.0.1",
                Port = probe.Port,
            };
        }
        else if (probe.Type == "command")
        {
            probeV1.Exec = new V1ExecAction(HandlerCmd(probe.Cmd));
        }
        else
        {
            throw new InvalidParameterException($"probe.Type is invalid:{probe.Type}");
        }

        return probeV1;
    }

    private static IList<string> HandlerCmd(string cmd)
    {
        return cmd.Split(
                new string[] { " ", "\r\n" }, StringSplitOptions.RemoveEmptyEntries
            )
            .Select(x => x.Trim())
            .ToList();
    }

    #endregion


    public static V1ObjectMeta CreateObjectMeta
    (string nsName, AppEditCommand command)
    {
        var metadata = new V1ObjectMeta
        {
            Name = command.AppName,
            NamespaceProperty = nsName,
            Annotations = new Dictionary<string, string>
            {
                [K8sLabelsConstants.LabelRegion] = command.Region,
                [K8sLabelsConstants.LabelRoom] = command.Room,
                [K8sLabelsConstants.LabelAppGrpName] = command.GrpName
            },
            Labels = new Dictionary<string, string>
            {
                [K8sLabelsConstants.LabelAppGrpName] = command.GrpName,
                [K8sLabelsConstants.LabelIamId] = command.IamId + "",
                [K8sLabelsConstants.LabelEnv] = command.Env
            }
        };

        if (!string.IsNullOrWhiteSpace(command.TypeName))
        {
            metadata.Labels.Add(K8sLabelsConstants.LabelAppType, command.TypeName);
        }


        return metadata;
    }

    public static V1LabelSelector GetV1LabelSelector
        (string nsName, AppEditCommand command)
    {
        return new V1LabelSelector
        {
            MatchLabels = new Dictionary<string, string>
            {
                [K8sLabelsConstants.LabelApp] = command.AppName,
                [K8sLabelsConstants.LabelAppType] = command.TypeName,
            }
        };
    }

}
