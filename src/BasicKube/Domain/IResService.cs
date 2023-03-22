namespace BasicKube.Api.Domain.App;

public interface IResService<TGrpInfo, TResDetails, TEditCmd>
{
    /// <summary>
    /// 列出服务组简介列表
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="appName"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    public Task<IEnumerable<TGrpInfo>> ListGrpAsync(int iamId);


    /// <summary>
    /// 列出服务组详情列表
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="svcGrpName"></param>
    /// <param name="env"></param>
    /// <returns></returns>
    public Task<IEnumerable<TResDetails>> ListAsync(int iamId, string grpName, string? env = null);

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
    /// 删除
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="svcName"></param>
    /// <returns></returns>
    public Task DelAsync(int iamId, string resName);

    /// <summary>
    /// 资源详情
    /// </summary>
    /// <param name="iamId"></param>
    /// <param name="svcName"></param>
    /// <returns></returns>
    public Task<TEditCmd?> DetailsAsync(int iamId, string svcName);
}
