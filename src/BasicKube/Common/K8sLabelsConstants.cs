namespace BasicKube.Api.Common;

public class RouteConstants
{
    public const string IamId = "iamId";
    public const string NsName = "nsName";
    public const string Env = "env";
}

public class K8sLabelsConstants
{
    public const string NsName = "nsName";

    public const string LabelIamId = "basickube/iamId";
    public const string LabelRegion = "basickube/region";
    public const string LabelRoom = "basickube/room";
    public const string LabelEnv = "basickube/env";

    public const string LabelGrpName = "basickube/grp";
    public const string LabelAppType = "basickube/type";

    public const string LabelApp = "app";
    public const string LabelDeployName = "deployName";
}
