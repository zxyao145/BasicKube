namespace BasicKube.Api.Controllers;

[Route("[controller]/[action]")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public ActionResult Index()
    {
        return Redirect("~/swagger/index.html");
    }
}