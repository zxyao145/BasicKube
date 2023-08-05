using BasicKube.Web.Common;
using BasicKube.Web.Services;
using BasicKube.Web.Services.Http;
using BasicKube.Web.Services.JsIntertop;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BasicKube.Web.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
            });
            builder.Logging
                .AddFilter(
                    (provider, category, logLevel) =>
                    {
#if DEBUG
                        if (category.Contains("System.Net.Http.HttpClient"))
                        {
                            Console.WriteLine("category:" + category);
                            Console.WriteLine(logLevel);
                        }
#endif
                        return category.Contains("System.Net.Http.HttpClient")
                               && logLevel > LogLevel.Information;
                    }
                );

            builder.Services
                .AddKubeHttpClient()
                .AddJsInteroper()
                .AddAuthService()
                .AddAntDesign();

            builder.Services.AddScoped<CookieHandler>();

            builder.Services.AddOptions();
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<AuthenticationStateProvider, IamAuthStateProvider>();
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddBlazoredSessionStorage();

            var host = builder.Build();
            host.Services.UseBcdForm();
            await host.RunAsync();
        }
    }
}