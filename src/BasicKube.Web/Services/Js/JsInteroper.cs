using Microsoft.JSInterop;

namespace BasicKube.Web.Services.JsIntertop;


public static class JsInteroperEtx
{
    public static IServiceCollection AddJsInteroper(this IServiceCollection services)
    {
        services.AddScoped<JsInteroper>();
        return services;
    }

}

public class JsInteroper
{
    public IJSRuntime JsRuntime { get; set; }

    public JsInteroper(IJSRuntime jS)
    {
        JsRuntime = jS;
    }

    public async Task<string> ReadCookie(string name)
    {
        return await JsRuntime.InvokeAsync<string>("readCookie", name); ;
    }
}

