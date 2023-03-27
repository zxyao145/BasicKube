using System.Text.Json;
using System.Text;

namespace BasicKube.Web.Services.Http;

public class HttpHelper
{
    public static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public static StringContent GetJsonContent<T>(T data)
    {
        var json = JsonSerializer.Serialize(data);
        var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
        return jsonContent;
    }
}
