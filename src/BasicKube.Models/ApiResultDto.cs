using System.Text.Json.Serialization;

namespace BasicKube.Models;

public class ApiResultDto<T>
{
    /// <summary>
    /// 
    /// </summary>
    public int Code { set; get; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("msg")]
    public string? Msg { set; get; } = "";


    /// <summary>
    /// 
    /// </summary>
    public T? Data { get; set; }

    public bool IsSuccess()
    {
        return Code == 0;
    }
}
