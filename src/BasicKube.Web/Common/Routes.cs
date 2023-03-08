namespace BasicKube.Web.Common;

public static class Routes
{
    public static string DeployGrpPage { get; } = "pages/deployAppGrpList";
    public static string DaemonSetAppGrpPage { get; } = "pages/daemonSetAppGrpList";
    public static string JobGrpPage { get; } = "pages/jobGrpList";
    public static string CronJobGrpPage { get; } = "pages/cronJobGrpList";


    public static string SvcGrpPage { get; } = "pages/svcGrpList";

    public static string IngGrpPage { get; } = "pages/ingressGrpList";


    public static string TerminalPage { get; } = "pages/terminal";
}
