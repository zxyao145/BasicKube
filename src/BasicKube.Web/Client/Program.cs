using BasicKube.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

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
            builder.Services.AddHttpClient<DeployHttp>();
            builder.Services.AddHttpClient<DaemonSetHttp>();
            builder.Services.AddHttpClient<ScalerHttp>();
            builder.Services.AddHttpClient<EventsHttp>();
            builder.Services.AddHttpClient<SvcHttp>();
            builder.Services.AddHttpClient<PodHttp>();
            builder.Services.AddHttpClient<IngHttp>();
            builder.Services.AddHttpClient<JobHttp>();
            builder.Services.AddScoped<KubeHttpClient>();
            builder.Services.AddAntDesign();

            var host = builder.Build();
            host.Services.UseBcdForm();
            await host.RunAsync();
        }
    }
}