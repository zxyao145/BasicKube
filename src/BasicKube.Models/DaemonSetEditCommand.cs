namespace BasicKube.Models;
#nullable disable

public class DaemonSetEditCommand : AppEditCommand
{
    public static string Type => "daemon-set";

    public override string TypeName => Type;
}