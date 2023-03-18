﻿namespace BasicKube.Models;
#nullable disable

public class DaemonSetEditCommand : AppEditCommand
{
    public static string Type => "daemon-set";

    public override string TypeName => Type;

    /// <summary>
    /// DeployName
    /// </summary>
    public string DaemonSetName
    {
        get => AppName;
        set => AppName = value;
    }
}