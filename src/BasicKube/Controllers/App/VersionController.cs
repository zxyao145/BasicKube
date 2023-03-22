using BasicKube.Api.Common.Components.ActionResultExtensions;

namespace BasicKube.Api.Controllers.App;

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
