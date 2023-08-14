namespace BasicKube.Web.Common;

public static class Routes
{
    #region kube

    public static string DeployGrpPage { get; } = "pages/kube/deployAppGrpList";
    public static string DaemonSetAppGrpPage { get; } = "pages/kube/daemonSetAppGrpList";
    public static string JobGrpPage { get; } = "pages/kube/jobGrpList";
    public static string CronJobGrpPage { get; } = "pages/kube/cronJobGrpList";

    public static string SvcGrpPage { get; } = "pages/kube/svcGrpList";

    public static string IngGrpPage { get; } = "pages/kube/ingressGrpList";

    public static string TerminalPage { get; } = "pages/kube/terminal";

    #endregion

}
