using Serilog;

namespace BasicKube.Api.Common.Components.Logger;

public static class HostBuilderExt
{
    public static Serilog.ILogger AddSerilog(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog((context, configuration) =>
        {
            ReadFromConfigFile(configuration);
        }, preserveStaticLogger: true);

        return Log.Logger;
    }

    private static void ReadFromConfigFile(LoggerConfiguration loggerConfiguration)
    {
        loggerConfiguration
            .Enrich.FromLogContext();

        var environmentName = Environment
            .GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "";
        var cfgHelper = new ConfigurationHelper();
        var cfg = cfgHelper.LoadJsonConfig("logger", environmentName);
        if (cfg != null)
        {
            loggerConfiguration
                .ReadFrom.Configuration(cfg);
        }
    }

}
