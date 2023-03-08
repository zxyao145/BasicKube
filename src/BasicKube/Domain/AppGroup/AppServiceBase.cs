using BasicKube.Api.Common;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.SignalR;
using Org.BouncyCastle.Security;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using static KubeClient.K8sAnnotations;

namespace BasicKube.Api.Domain.App;

public abstract class AppServiceBase<TAppDetails, TEditCmd>
    : IAppService<TAppDetails, TEditCmd>
    where TAppDetails : AppDetailsQuery
    where TEditCmd : AppCreateCommand
{

    protected readonly IamService IamService;

    protected AppServiceBase(IamService iamService)
    {
        IamService = iamService;
    }

    public abstract Task CreateAsync(int iamId, TEditCmd cmd);
    public abstract Task DelAsync(int iamId, string appName);
    public abstract Task<TEditCmd?> DetailsAsync(int iamId, string appName);
    public abstract Task<IEnumerable<TAppDetails>> ListAsync(int iamId, string appName, string? evn = null);
    public abstract Task PublishAsync(int iamId, AppPublishCommand cmd);
    public abstract Task UpdateAsync(int iamId, TEditCmd cmd);

    #region CreateKubeApp

    public static TKubeObj CreateKubeApp<TKubeObj>(string nsName, AppCreateCommand command)
        where TKubeObj : IKubernetesObject<V1ObjectMeta>, IValidate
    {
        var obj = Activator.CreateInstance<TKubeObj>();
        Debug.Assert(obj != null);
        if (command is DeployCreateCommand deployCreateCmd)
        {
            var spec = new V1DeploymentSpec();
            (obj as V1Deployment)!.Spec = spec;
            spec.Selector = GetV1LabelSelector(nsName, command);
            spec.Template = GetPodTemplateSpec(nsName, command);

            spec.Replicas = deployCreateCmd.Replicas < 0
                ? 0 : deployCreateCmd.Replicas;
        }
        else if (command is DaemonSetCreateCommand daemonSetCreateCommand)
        {
            var spec = new V1DaemonSetSpec();
            (obj as V1DaemonSet)!.Spec = spec;
            spec.Selector = GetV1LabelSelector(nsName, command);
            spec.Template = GetPodTemplateSpec(nsName, command);
        }

        Debug.Assert(obj != null);
        obj.Metadata = CreateObjectMeta(nsName, command);

        obj.Validate();
#if DEBUG
        var yaml = KubernetesYaml.Serialize(obj);
#endif
        return obj;
    }

    public static V1ObjectMeta CreateObjectMeta
        (string nsName, AppCreateCommand command)
    {
        var metadata = new V1ObjectMeta
        {
            Name = command.AppName,
            NamespaceProperty = nsName,
            Annotations = new Dictionary<string, string>
            {
                [Constants.LableRegion] = command.Region,
                [Constants.LableRoom] = command.Room,
                [Constants.LableAppGrpName] = command.GrpName
            },
            Labels = new Dictionary<string, string>
            {
                [Constants.LableAppGrpName] = command.GrpName,
                [Constants.LableIamId] = command.IamId + "",
                [Constants.LableEnv] = command.Env
            }
        };

        if (!string.IsNullOrWhiteSpace(command.TypeName))
        {
            metadata.Labels.Add(Constants.LableAppType, command.TypeName);
        }


        return metadata;
    }

    public static V1LabelSelector GetV1LabelSelector
        (string nsName, AppCreateCommand command)
    {
        return new V1LabelSelector
        {
            MatchLabels = new Dictionary<string, string>
            {
                [Constants.LableApp] = command.AppName,
                [Constants.LableAppType] = command.TypeName,
            }
        };
    }

    #endregion

    #region PodTemplateSpec

    public static V1PodTemplateSpec GetPodTemplateSpec
        (string nsName, AppCreateCommand command, string type = "")
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
                    [Constants.LableRegion] = command.Region,
                    [Constants.LableRoom] = command.Room,
                },
                Labels = new Dictionary<string, string>
                {
                    [Constants.LableIamId] = command.IamId + "",
                    [Constants.LableEnv] = command.Env,

                    // pod 独有的
                    [Constants.LableApp] = command.AppName,
                }
            },
            Spec = new V1PodSpec()
        };

        if (!string.IsNullOrWhiteSpace(type))
        {
            podTemp.Metadata.Labels.Add(Constants.LableAppType, type);
        }

        podTemp.Spec.RestartPolicy = command.RestartPolicy;
        podTemp.Spec.Containers = CreateContainers(nsName, command);
        return podTemp;
    }

    private static IList<V1Container> CreateContainers
        (string nsName, AppCreateCommand command)
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


    public static TCmd GetAppCreateCommand<TCmd>
        (string deployUnitName, IKubernetesObject<V1ObjectMeta> kubeApp)
        where TCmd : AppCreateCommand
    {
        var obj = Activator.CreateInstance<TCmd>();
        Debug.Assert(obj != null);

        obj.GrpName = kubeApp.Metadata.Labels[Constants.LableAppGrpName];
        obj.AppName = deployUnitName;
        obj.Env = kubeApp.Metadata.Labels[Constants.LableEnv];
        obj.IamId = int.Parse(kubeApp.Metadata.Labels[Constants.LableIamId] ?? "0");
        obj.Region = kubeApp.Metadata.Annotations[Constants.LableRegion];
        obj.Room = kubeApp.Metadata.Annotations[Constants.LableRoom];
        
        if(obj is DeployCreateCommand deployCreateCommand)
        {
            var app = kubeApp as V1Deployment;
            Debug.Assert(app != null);

            obj.Containers = 
                GetContainerInfos(app.Spec.Template.Spec.Containers);
            deployCreateCommand.Replicas = app.Spec.Replicas ?? 0;
        }
        else if (obj is DaemonSetCreateCommand)
        {
            var app = kubeApp as V1DaemonSet;
            Debug.Assert(app != null);

            obj.Containers =
                GetContainerInfos(app.Spec.Template.Spec.Containers);
        }

        return obj;
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
}
