
namespace BasicKube.Api.Domain.App;


public interface IAppService<TGrpInfo, TResDetails, TEditCmd>
    : IResService<TGrpInfo, TResDetails, TEditCmd>
    where TResDetails : AppDetailsQuery
{
    /// <summary>
    /// 更新应用
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="cmd"></param>
    /// <returns></returns>
    public Task PublishAsync(int iamId, AppPublishCommand cmd);
}
