using Microsoft.Extensions.Configuration;

namespace BasicKube.Api.Domain;

public class IamService
{
    private readonly IConfiguration _configuration;
    private readonly IConfigurationSection _nameSpaceMap;

    public IamService(IConfiguration configuration)
    {
        _configuration = configuration;
        _nameSpaceMap = _configuration.GetSection("NameSpaceMap");
    }

    public string GetNsName(int iamId)
    {
        return GetNsName(iamId + "");
    }

    public string GetNsName(string iamId)
    {
        var nsName = "default";
        if (_configuration != null)
        {
            if (_nameSpaceMap != null)
            {
                nsName = _nameSpaceMap.GetSection(iamId)?.Value ?? "default";
            }
        }

        return nsName;
    }
}
