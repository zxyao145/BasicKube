namespace BasicKube.Api.Common.Components;

/// <summary>
/// 配置帮助类
/// </summary>
public class ConfigurationHelper
{
    private string _configFileDir;
    public ConfigurationHelper(string configFileDir = "configs")
    {
        _configFileDir = configFileDir;
    }


    /// <summary>
    /// 根据配置文件名称（不带扩展名）加载指定的配置项
    /// </summary>
    /// <param name="configFileName">
    /// 配置文件名称（不带扩展名），
    /// 使用约定，配置文件放在项目的Config目录中，如logging配置：configs/logging.json
    /// </param>
    /// <param name="environmentName"></param>
    /// <param name="reloadOnChange">自动更新</param>
    /// <returns></returns>
    public IConfiguration? LoadJsonConfig(string configFileName, string environmentName = "", bool reloadOnChange = false)
    {
        var fileDir = Path.Combine(AppContext.BaseDirectory, _configFileDir);
        if (!Directory.Exists(fileDir))
            return null;

        var defaultConfigFileName = "default/" + configFileName;

        var builder = new ConfigurationBuilder()
           .SetBasePath(fileDir)
           .AddJsonFile(defaultConfigFileName + ".json", true, reloadOnChange);
        if (!string.IsNullOrEmpty(environmentName))
        {
            builder.AddJsonFile(defaultConfigFileName + "." + environmentName + ".json", true, reloadOnChange);
        }

        builder.AddJsonFile(configFileName + ".json", true, reloadOnChange);
        if (!string.IsNullOrEmpty(environmentName))
        {
            builder.AddJsonFile(configFileName + "." + environmentName + ".json", true, reloadOnChange);
        }

        return builder.Build();
    }

    /// <summary>
    /// 根据配置文件名称（不带扩展名）加载指定的配置项
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="configFileName">配置文件名称，使用约定，配置文件放在项目的config目录中，如logging配置：config/logging.json</param>
    /// <param name="environmentName"></param>
    /// <param name="reloadOnChange">自动更新</param>
    /// <returns></returns>
    public T Get<T>(string configFileName,
        string environmentName = "",
        bool reloadOnChange = false)
    {
        var configuration = LoadJsonConfig(configFileName, environmentName, reloadOnChange);
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration.Get<T>();
    }
}
