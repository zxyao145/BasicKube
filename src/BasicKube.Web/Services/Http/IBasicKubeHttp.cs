namespace BasicKube.Web.Services.Http;

public interface IBasicKubeAppHttp<TGrpInfo, TDetails, TCmd>
    : IBasicKubeHttp<TGrpInfo, TDetails, TCmd>
    where TCmd : IIamModel
{
    Task<bool> Publish(int iamId, AppPublishCommand cmd);
}

public interface IBasicKubeHttp<TGrpInfo, TDetails, TCmd>
    where TCmd : IIamModel
{
    Task<List<TGrpInfo>> ListGrp(int iamId);

    Task<List<TDetails>> List(int iamId, string? grpName = null, string? env = null);

    Task<bool> Create(TCmd command);

    Task<bool> Update(TCmd command);

    Task<bool> Del(int iamId, string svcName);

    Task<TCmd?> Details(int iamId, string svcName);
}

