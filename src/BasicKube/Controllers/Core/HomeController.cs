namespace BasicKube.Api.Controllers.Core;

[Route("[controller]/[action]")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public ActionResult Index()
    {
        return Redirect("~/swagger/index.html");
    }

    [HttpGet]
    public ActionResult Healthz()
    {
        return Ok(new
        {
            Time = DateTime.Now,
        });
    }
}