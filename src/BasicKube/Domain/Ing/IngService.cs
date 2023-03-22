using BasicKube.Api.Domain.App;

namespace BasicKube.Api.Domain.Ing;

public interface IIngService
    : IResService<IngGrpInfo, IngDetails, IngEditCommand>
{
    Task<IEnumerable<IngDetails>> ListInEnvAsync(int iamId, string env);
}

[Service<IIngService>]
public class IngService : IIngService
{
    private readonly ILogger<IngService> _logger;
    private readonly KubernetesFactory _k8sFactory;
    private readonly IamService _iamService;

    public IngService(ILogger<IngService> logger,
        KubernetesFactory kubernetes,
        IamService iamService)
    {
        _logger = logger;
        _k8sFactory = kubernetes;
        _iamService = iamService;
    }

    public async Task<IEnumerable<IngGrpInfo>> ListGrpAsync(int iamId)
    {
        var label = $"{K8sLabelsConstants.LabelIamId}={iamId}";

        var result = new List<V1Ingress>();
        foreach (var item in _k8sFactory.All)
        {
            var res = await item.Value
                .NetworkingV1
                .ListNamespacedIngressAsync(
                    _iamService.GetNsName(iamId),
                    labelSelector: label + $",{K8sLabelsConstants.LabelEnv}={item.Key}"
                );

            result.AddRange(res.Items);
        }

        return result
            .Select(x => x.Metadata.Labels[K8sLabelsConstants.LabelGrpName])
            .ToHashSet()
            .Select(x => new IngGrpInfo()
            {
                Name = x
            });
    }

    #region ListAsync

    private async Task<List<IngDetails>> ListOneEnv
       (int iamId, string env, string nsName, string grpName)
    {
        var label = $"{K8sLabelsConstants.LabelIamId}={iamId}" +
                $",{K8sLabelsConstants.LabelEnv}={env}";

        if (!string.IsNullOrWhiteSpace(grpName))
        {
            label += $",{K8sLabelsConstants.LabelGrpName}={grpName}";
        }

        var kubernetes = _k8sFactory.MustGet(env);
        var ingressRes = await kubernetes.NetworkingV1
           .ListNamespacedIngressAsync(nsName, labelSelector: label);

        var ingRes = new List<IngDetails>();
        foreach (var x in ingressRes.Items)
        {
            var details = GetIngDetails(x);
            ingRes.Add(details);
        }

        return ingRes;
    }

    public async Task<IEnumerable<IngDetails>> ListInEnvAsync
       (int iamId, string env)
    {
        var nsName = _iamService.GetNsName(iamId);
        return await ListOneEnv(iamId, env, nsName, "");
    }

    public async Task<IEnumerable<IngDetails>> ListAsync
         (int iamId, string grpName, string? env = null)
    {
        var nsName = _iamService.GetNsName(iamId);
        if (!string.IsNullOrWhiteSpace(env))
        {
            return await ListOneEnv(iamId, env, nsName, grpName);
        }

        var result = new List<IngDetails>();
        foreach (var item in _k8sFactory.All)
        {
            var temp = await ListOneEnv(iamId, item.Key, nsName, grpName);
            result.AddRange(temp);
        }
        return result;
    }

    private IngDetails GetIngDetails(V1Ingress x)
    {
        var details = new IngDetails()
        {
            Name = x.Metadata.Name,
            CreateTime = x.Metadata.CreationTimestamp,
            UpdateTime = x.Metadata.CreationTimestamp,
            IngClassName = x.Spec.IngressClassName,
            LbIps = GetIps(x),
            Rules = GetRules(x),
        };

        return details;
    }

    private List<string> GetIps(V1Ingress v1Ingress)
    {
        return v1Ingress.Status
            .LoadBalancer
            .Ingress
            .Select(x => x.Ip).ToList();
    }

    private List<IngRuleOptions> GetRules(V1Ingress v1Ingress)
    {
        return v1Ingress.Spec.Rules.Select((x, ruleIndex) =>
        {
            var rules = x.Http.Paths.Select((x, valIndex) =>
            {
                return new IngRuleValue()
                {
                    Index = valIndex,
                    PathType = x.PathType,
                    Path = x.Path,
                    TargetService = x.Backend.Service.Name,
                    Port = x.Backend.Service.Port.Number,
                };
            })
            .ToList();
            var host = new IngRuleOptions
            {
                Index = ruleIndex,
                Host = x.Host,
                RuleValues = rules
            };

            return host;
        }).ToList();
    }

    #endregion ListAsync


    #region edit

    public async Task CreateAsync(int iamId, IngEditCommand command)
    {
        var cmd = Cmd2ResObj(iamId, command);
        cmd.Validate();
        await _k8sFactory.MustGet(command.Env)
            .NetworkingV1
            .CreateNamespacedIngressAsync(cmd, cmd.Metadata.NamespaceProperty);
    }

    public async Task UpdateAsync(int iamId, IngEditCommand command)
    {
        var ingress = Cmd2ResObj(iamId, command);
        await _k8sFactory.MustGet(command.Env)
            .NetworkingV1
            .ReplaceNamespacedIngressAsync(
            ingress,
            ingress.Metadata.Name,
            ingress.Metadata.NamespaceProperty);
    }

    private V1Ingress Cmd2ResObj(
        int iamId,
        IngEditCommand command)
    {
        string nsName = _iamService.GetNsName(iamId);
        var ing = new V1Ingress()
        {
            Metadata = new V1ObjectMeta()
            {
                Name = command.IngName,
                NamespaceProperty = nsName,
                Labels = new Dictionary<string, string>()
                {
                    { K8sLabelsConstants.LabelGrpName, command.IngGrpName },
                    { K8sLabelsConstants.LabelEnv, command.Env },
                    { K8sLabelsConstants.LabelIamId, iamId + "" },
                }
            },
            Spec = new V1IngressSpec()
            {
                IngressClassName = command.IngClassName,
                Rules = new List<V1IngressRule>()
            }
        };
        List<V1IngressRule> hostRules = ParseHttpRule(command);

        ing.Spec.Rules = hostRules;
        return ing;
    }

    private static List<V1IngressRule> ParseHttpRule(IngEditCommand command)
    {
        return command.Rules.Select(
                    x =>
                    {
                        var rule = new V1IngressRule()
                        {
                            Host = x.Host,
                            Http = ParseHttp(x)
                        };

                        return rule;
                    }
                    )
                    .ToList();
    }

    private static V1HTTPIngressRuleValue ParseHttp(IngRuleOptions x)
    {
        return new V1HTTPIngressRuleValue
        {
            Paths = x.RuleValues.Select(v => new V1HTTPIngressPath
            {
                PathType = v.PathType,
                Path = v.Path,
                Backend = new V1IngressBackend()
                {
                    Service = new V1IngressServiceBackend
                    {
                        Name = v.TargetService,
                        Port = new V1ServiceBackendPort
                        {
                            Number = v.Port,
                        }
                    }
                }
            }).ToList(),
        };
    }

    #endregion edit


    public async Task<IngEditCommand?> DetailsAsync(int iamId, string ingName)
    {
        var nsName = _iamService.GetNsName(iamId);

        var ing = await _k8sFactory.MustGetByAppName(ingName)
            .NetworkingV1
            .ReadNamespacedIngressAsync(ingName, nsName);

        if (ing == null)
        {
            return null;
        }

        var cmd = new IngEditCommand();
        cmd.IngGrpName = ing.Metadata.Labels[K8sLabelsConstants.LabelGrpName];
        cmd.Env = ing.Metadata.Labels[K8sLabelsConstants.LabelEnv];
        cmd.IngClassName = ing.Spec.IngressClassName;
        cmd.Rules = GetRules(ing);

        return cmd;
    }


    public async Task DelAsync(int iamId, string ingName)
    {
        var nsName = _iamService.GetNsName(iamId);
        var ing = await _k8sFactory.MustGetByAppName(ingName)
            .NetworkingV1
            .DeleteNamespacedIngressAsync(ingName, nsName);
    }
}
