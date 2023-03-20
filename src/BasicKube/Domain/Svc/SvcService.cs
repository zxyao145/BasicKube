using BasicKube.Api.Domain.App;
using BasicKube.Api.Domain.Pod;

namespace BasicKube.Api.Domain.Svc;

public interface ISvcService
    : IResService<SvcGrpInfo, SvcDetails, SvcEditCommand>
{
    Task<IEnumerable<SvcDetails>> ListInEnvAsync(int iamId, string env);
}

[Service<ISvcService>]
public class SvcService : ISvcService
{
    private readonly KubernetesFactory _k8sFactory;
    private readonly ILogger<SvcService> _logger;
    private readonly IamService _iamService;

    public SvcService(
        KubernetesFactory kubernetes,
        ILogger<SvcService> logger,
        IamService iamService)
    {
        _k8sFactory = kubernetes;
        _logger = logger;
        _iamService = iamService;
    }


    #region edit

    public async Task CreateAsync(int iamId, SvcEditCommand cmd)
    {
        var svc = CmdToSvc(cmd, iamId);
        await _k8sFactory.MustGet(cmd.Env)
            .CoreV1
            .CreateNamespacedServiceAsync(svc, svc.Metadata.NamespaceProperty);
    }

    public async Task UpdateAsync(int iamId, SvcEditCommand cmd)
    {
        var svc = CmdToSvc(cmd, iamId);

        await _k8sFactory.MustGet(cmd.Env)
            .CoreV1
            .ReplaceNamespacedServiceAsync(svc,
                svc.Metadata.Name,
                svc.Metadata.NamespaceProperty);
    }


    private V1Service CmdToSvc(
        SvcEditCommand command,
        int iamId
        )
    {
        string nsName = _iamService.GetNsName(iamId);
        var svc = new V1Service
        {
            Metadata = new V1ObjectMeta()
            {
                Name = $"{command.SvcGrpName}-{command.Env}",
                NamespaceProperty = nsName,
                Labels = new Dictionary<string, string>()
                {
                    { K8sLabelsConstants.LabelGrpName, command.SvcGrpName },
                    { K8sLabelsConstants.LabelEnv, command.Env },
                    { K8sLabelsConstants.LabelIamId, iamId + "" },
                }
            },
            Spec = new V1ServiceSpec()
            {
                Type = command.Type
            }
        };
        svc.Spec.Ports = new List<V1ServicePort>(command.Ports.Count);

        command.Ports.ForEach(item =>
        {
            svc.Spec.Ports
                .Add(new V1ServicePort()
                {
                    Port = item.Port,
                    Protocol = item.Protocol,
                    TargetPort = item.TargetPort,
                    NodePort = item.NodePort,
                });
        });

        svc.Spec.Selector = new Dictionary<string, string>();
        var appName = command.RelationAppName;
        svc.Spec.Selector.Add(K8sLabelsConstants.LabelEnv, command.Env);
        svc.Spec.Selector.Add(K8sLabelsConstants.LabelApp, appName);
        svc.Spec.Selector.Add(K8sLabelsConstants.LabelIamId, iamId + "");
        //svc.Spec.Selector.Add(K8sLabelsConstants.LabelAppType, DeployEditCommand.Type);
        return svc;
    }

    #endregion


    public async Task DelAsync(int iamId, string svcName)
    {
        string nsName = _iamService.GetNsName(iamId);
        var res = await _k8sFactory
            .MustGetByAppName(svcName)
           .CoreV1
           .DeleteNamespacedServiceAsync(svcName, nsName);

        _logger.LogInformation("Del end:{0}", res);
    }

    public async Task<SvcEditCommand?> DetailsAsync(int iamId, string svcName)
    {
        var nsName = _iamService.GetNsName(iamId);

        var svc = await _k8sFactory
            .MustGetByAppName(svcName)
            .CoreV1
            .ReadNamespacedServiceAsync(svcName, nsName);

        if (svc == null)
        {
            return null;
        }

        var cmd = new SvcEditCommand();
        cmd.SvcGrpName = svc.Metadata.Labels[K8sLabelsConstants.LabelGrpName];
        cmd.Env = svc.Metadata.Labels[K8sLabelsConstants.LabelEnv];
        cmd.Type = svc.Spec.Type;
        cmd.RelationAppName = string.Join("-", svc.Spec.Selector[K8sLabelsConstants.LabelApp].Split("-")[0..^1]);
        cmd.Ports = svc.Spec.Ports.Select((x, i) =>
        {
            var p = new SvcPortInfo();
            p.NodePort = x.NodePort;
            p.TargetPort = Convert.ToInt32(x.TargetPort.Value);
            p.Protocol = x.Protocol;
            p.Port = x.Port;
            p.Index = i;
            return p;
        }).ToList();

        return cmd;
    }

    public async Task<IEnumerable<SvcGrpInfo>> ListGrpAsync(int iamId)
    {
        var label = $"{K8sLabelsConstants.LabelIamId}={iamId}";
        var nsName = _iamService.GetNsName(iamId);
        var res = new List<V1Service>();
        foreach (var item in _k8sFactory.All)
        {
            var service = await
                item.Value
                .CoreV1
                .ListNamespacedServiceAsync(
                      nsName,
                      labelSelector: label + $",{K8sLabelsConstants.LabelEnv}={item.Key}"
                  );
            res.AddRange(service.Items);
        }

        return res
            .Select(x => x.Metadata.Labels[K8sLabelsConstants.LabelGrpName])
            .ToHashSet()
            .Select(x => new SvcGrpInfo()
            {
                Name = x
            });
    }

    public async Task<IEnumerable<SvcDetails>> ListAsync
        (int iamId, string grpName, string? env = null)
    {
        var nsName = _iamService.GetNsName(iamId);

        if (!string.IsNullOrWhiteSpace(env))
        {
            return await ListOneEnv(iamId, env, nsName, grpName);
        }

        var result = new List<SvcDetails>();
        foreach (var item in _k8sFactory.All)
        {
            var temp = await ListOneEnv(iamId, item.Key, nsName, grpName);
            result.AddRange(temp);
        }
        return result;

    }

    public async Task<IEnumerable<SvcDetails>> ListInEnvAsync(int iamId, string env)
    {
        var nsName = _iamService.GetNsName(iamId);
        return await ListOneEnv(iamId, env, nsName, "");
    }

    private async Task<List<SvcDetails>> ListOneEnv
        (int iamId, string env, string nsName, string grpName)
    {
        var label = $"{K8sLabelsConstants.LabelIamId}={iamId}" +
            $",{K8sLabelsConstants.LabelEnv}={env}";

        if (!string.IsNullOrWhiteSpace(grpName))
        {
            label += $",{K8sLabelsConstants.LabelGrpName}={grpName}";
        }

        var kubernetes = _k8sFactory.MustGet(env);
        var service = await kubernetes.CoreV1
            .ListNamespacedServiceAsync(nsName, labelSelector: label);

        var services = new List<SvcDetails>();
        foreach (var x in service.Items)
        {
            SvcDetails svcInfo = await GetSvcInfo(kubernetes, iamId, nsName, x);
            services.Add(svcInfo);
        }

        return services;
    }


    private async Task<SvcDetails> GetSvcInfo(IKubernetes kubernetes, int iamId, string nsName, V1Service? x)
    {
        var svcInfo = new SvcDetails()
        {
            Name = x.Metadata.Name, //x.Metadata.Name.Split("-")[0],
            IamId = iamId,
            ClusterIp = x.Spec.ClusterIP,
            Type = x.Spec.Type,
            CreateTime = x.Metadata.CreationTimestamp,
            Selectors = x.Spec.Selector.Select(k => new Selector()
            {
                Key = k.Key,
                Value = k.Value
            }).ToList(),
            Ports = x.Spec.Ports.Select((p, i) =>
            {
                var info = new SvcPortInfo()
                {
                    Index = i,
                    Port = p.Port,
                    Protocol = p.Protocol,
                    TargetPort = Convert.ToInt32(p.TargetPort.Value),
                    NodePort = p.NodePort,
                };

                return info;
            }).ToList(),
        };
        if (x.Metadata.DeletionTimestamp != null)
        {
            svcInfo.Status = "Terminating";
        }
        var podLabel = string.Join(",", svcInfo.Selectors.Select(x => $"{x.Key}={x.Value}"));
        var pods = await kubernetes
            .CoreV1
            .ListNamespacedPodAsync(nsName, labelSelector: podLabel);
        svcInfo.PodDetails = pods.Items.Select(x =>
        {
            return PodService.GetPodDetail(x, false);
        });
        return svcInfo;
    }
}
