using BasicKube.Api.Config;
using Microsoft.Extensions.Options;

namespace BasicKube.Api.Domain;

[Service]
public class IamService
{
    private readonly K8sOptions _k8sOptions;

    public IamService(IOptions<K8sOptions> options)
    {
        _k8sOptions = options.Value ;
    }

    public string GetNsName(int iamId)
    {
        return GetNsName(iamId + "");
    }

    public string GetNsName(string iamId)
    {
        var nsName = "default";
        if (_k8sOptions.NameSpaceMap.ContainsKey(iamId))
        {
            nsName = _k8sOptions.NameSpaceMap[iamId];
        }

        return nsName;
    }
}
