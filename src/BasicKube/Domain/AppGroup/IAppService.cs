
namespace BasicKube.Api.Domain.App;

public interface IAppService
{
}

public interface IAppService<TAppDetails, TEditCmd>: IAppService
{
    /// <summary>
    /// 列出应用列表
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="appName"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    public Task<IEnumerable<TAppDetails>> ListAsync(int iamId, string appName, string? env = null);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="cmd"></param>
    /// <returns></returns>
    public Task CreateAsync(int iamId, TEditCmd cmd);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="cmd"></param>
    /// <returns></returns>
    public Task UpdateAsync(int iamId, TEditCmd cmd);

    /// <summary>
    /// 更新应用
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="cmd"></param>
    /// <returns></returns>
    public Task PublishAsync(int iamId, AppPublishCommand cmd);

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="appName"></param>
    /// <returns></returns>
    public Task DelAsync(int iamId, string appName);

    /// <summary>
    /// 资源详情
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="appName"></param>
    /// <returns></returns>
    public Task<TEditCmd?> DetailsAsync(int iamId, string appName);
}
