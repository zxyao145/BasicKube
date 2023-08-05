namespace BasicKube.Models;

public class PageListDto<T>
{
    private static readonly PageListDto<T> _empty = new PageListDto<T>();
    public static PageListDto<T> Empty => _empty;


#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public PageListDto()
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pageIndex">当前页</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="total">总记录数</param>
    /// <param name="data">当前页数据</param>
    public PageListDto(
        int pageIndex, int pageSize,
        int total, ICollection<T> data)
    {
        int pageCount = (int)Math.Ceiling(total / (float)pageSize);

        PageIndex = pageIndex;
        PageCount = pageCount;
        Total = total;
        PageSize = pageSize;
        Data = data ?? new List<T>();
    }

    /// <summary>
    /// 当前页
    /// </summary>
    public long PageIndex { get; set; }

    /// <summary>
    /// 每页大小
    /// </summary>
    public long PageSize { get; set; }

    /// <summary>
    /// 总页面数
    /// </summary>
    public long PageCount { get; set; }

    ///// <summary>
    ///// 总记录数
    ///// </summary>
    public long Total { get; set; }

    public ICollection<T> Data { get; set; } = new List<T>();
}
