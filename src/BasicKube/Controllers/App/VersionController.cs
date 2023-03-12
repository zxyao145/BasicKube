namespace BasicKube.Api.Controllers;

public class VersionController
{
    [HttpGet("{resName}")]
    public ActionResult GetVersionList
        (
         [FromRoute] string resName
        )
    {
        return ApiResult.Success;
    }
}
