

namespace BasicKube.Models;

public class ContainerInfo
{
    public int Index { get; set; }
    public string Name { get; set; } = "main";
    public string Image { get; set; } = "";
    public string Tag { get; set; } = "";

    public List<PortInfo> Ports { get; set; } = new List<PortInfo>();

    #region 规格

    // 核心数（最小0.1）
    public double Cpu { get; set; } = 0.1;

    // 内存数（GB）
    public double Memory { get; set; } = 0.5;

    #endregion 规格

    #region 生命周期

    public string StartCmd { get; set; } = "";
    public string AfterStart { get; set; } = "";
    public string BeforeStop { get; set; } = "";

    #endregion 生命周期

    #region 健康检查

    public Probe? LivenessProbe { get; set; } = null;
    public Probe? ReadinessProbe { get; set; } = null;
#nullable disable

    #endregion 健康检查

    /// <summary>
    /// 环境变量
    /// </summary>
    public List<EnvVarInfo> EnvVars { get; set; } = new List<EnvVarInfo>();
}

public record PortInfo
{
    public int Index { get; set; }

    public int Port { get; set; }
    public string Protocol { get; set; }
}

public record EnvVarInfo
{
    public int Index { get; set; }

    public string Key { get; set; }
    public string Value { get; set; }
}

public class Probe
{
    public string Type { get; set; }

    #region Http

    public string Path { get; set; }
    public Dictionary<string, string> Header { get; set; } = new Dictionary<string, string>();

    #endregion Http

    /// <summary>
    /// http or tcp
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// command type
    /// </summary>
    public string Cmd { get; set; }

    /// <summary>
    /// 探测周期
    /// </summary>
    public int PeriodSeconds { get; set; } = 10;

    /// <summary>
    /// 超时时间
    /// </summary>
    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// 失败阈值
    /// </summary>
    public int FailureThreshold { get; set; } = 3;

    /// <summary>
    /// 第一次等待时间
    /// </summary>
    public int InitialDelaySeconds { get; set; } = 5;
}