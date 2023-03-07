namespace BasicKube.Api.Controllers;

public class VersionController
{
    [HttpGet("{deployUnitName}")]
    public ActionResult GetVersionList
        (
         [FromRoute] string deployUnitName
        )
    {
        return ApiResult.Success;
    }
}
