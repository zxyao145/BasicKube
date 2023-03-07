using BasicKube.Api.Common;
using k8s;
using System.Linq;

namespace BasicKube.Api.Domain.Ing;

public interface IIngService
{
    /// <summary>
    /// 列出Ing组简介列表
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="appName"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    public Task<IEnumerable<IngGrpInfo>> ListGrpAsync(int iamId);


    /// <summary>
    /// 列出Ing服务组详情列表
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="ingGrpName"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    public Task<IEnumerable<IngDetails>> ListAsync(int iamId, string grpName, string? env = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="cmd"></param>
    /// <returns></returns>
    public Task CreateAsync(int iamId, IngEditCommand cmd);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="cmd"></param>
    /// <returns></returns>
    public Task UpdateAsync(int iamId, IngEditCommand cmd);

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="ingName"></param>
    /// <returns></returns>
    public Task DelAsync(int iamId, string ingName);

    /// <summary>
    /// 资源详情
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="ingName"></param>
    /// <returns></returns>
    public Task<IngEditCommand?> DetailsAsync(int iamId, string ingName);
}

public class IngService : IIngService
{
    private readonly ILogger<IngService> _logger;
    private readonly IKubernetes _kubernetes;
    private readonly IamService _iamService;

    public IngService(ILogger<IngService> logger, 
        IKubernetes kubernetes, 
        IamService iamService)
    {
        _logger = logger;
        _kubernetes = kubernetes;
        _iamService = iamService;
    }

    public async Task<IEnumerable<IngGrpInfo>> ListGrpAsync(int iamId)
    {
        var label = $"{Constants.LableIamId}={iamId}";

        var service = await _kubernetes.NetworkingV1.ListNamespacedIngressAsync(
            _iamService.GetNsName(iamId),
            labelSelector: label
            );

        return service.Items
            .Select(x => x.Metadata.Labels[Constants.LabelIngGrpName])
            .ToHashSet()
            .Select(x => new IngGrpInfo()
            {
                Name = x
            });
    }

    #region ListAsync

    public async Task<IEnumerable<IngDetails>> ListAsync(int iamId, string grpName, string? env = null)
    {
        var nsName = _iamService.GetNsName(iamId);
        var label = $"{Constants.LableIamId}={iamId},{Constants.LabelIngGrpName}={grpName}";
        if (!string.IsNullOrWhiteSpace(env))
        {
            label += $",{Constants.LableEnv}={env}";
        }

        var ingressRes = await _kubernetes.NetworkingV1
            .ListNamespacedIngressAsync(nsName, labelSelector: label);

        var ingRes = new List<IngDetails>();
        foreach (var x in ingressRes.Items)
        {
            var details = GetIngDetails(x);
            ingRes.Add(details);
        }

        return ingRes;
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

    #endregion


    #region edit

    public async Task CreateAsync(int iamId, IngEditCommand command)
    {
        var cmd = Cmd2ResObj(iamId, command);
        cmd.Validate();
        await _kubernetes.NetworkingV1
            .CreateNamespacedIngressAsync(cmd, cmd.Metadata.NamespaceProperty);
    }

    public async Task UpdateAsync(int iamId, IngEditCommand command)
    {
        var ingress = Cmd2ResObj(iamId, command);
        await _kubernetes.NetworkingV1
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
                    { Constants.LabelIngGrpName, command.IngGrpName },
                    { Constants.LableEnv, command.Env },
                    { Constants.LableIamId, iamId + "" },
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

    #endregion


    public async Task<IngEditCommand?> DetailsAsync(int iamId, string ingName)
    {
        var nsName = _iamService.GetNsName(iamId);

        var ing = await _kubernetes.NetworkingV1
            .ReadNamespacedIngressAsync(ingName, nsName);

        if (ing == null)
        {
            return null;
        }

        var cmd = new IngEditCommand();
        cmd.IngGrpName = ing.Metadata.Labels[Constants.LabelIngGrpName];
        cmd.Env = ing.Metadata.Labels[Constants.LableEnv];
        cmd.IngClassName = ing.Spec.IngressClassName;
        cmd.Rules = GetRules(ing);

        return cmd;
    }


    public async Task DelAsync(int iamId, string ingName)
    {
        var nsName = _iamService.GetNsName(iamId);
        var ing = await _kubernetes.NetworkingV1
            .DeleteNamespacedIngressAsync(ingName, nsName);
    }
}
