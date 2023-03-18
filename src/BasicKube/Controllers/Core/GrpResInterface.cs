namespace BasicKube.Api.Controllers;

public interface IGrpResControllerWithoutPublish<in TCmd> where TCmd : class
{
    [HttpGet]
    Task<IActionResult> ListGrp();

    [HttpGet("{grpName?}")]
    Task<IActionResult> List([FromRoute] string grpName);

    [HttpPost]
    Task<IActionResult> Create([FromBody] TCmd command);

    [HttpPost]
    Task<IActionResult> Update([FromBody] TCmd command);

    [HttpGet("{appName}")]
    Task<IActionResult> Details([FromRoute] string appName);

    [HttpDelete("{appName}")]
    Task<IActionResult> Del([FromRoute] string appName);
}

public interface IGrpResController<TCmd> : IGrpResControllerWithoutPublish<TCmd>
    where TCmd : class
{
    [HttpPut]
    Task<IActionResult> Publish([FromBody] AppPublishCommand command);
}
