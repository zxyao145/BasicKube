using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace BasicKube.Api.Controllers.Account;

[ApiController]
[Route($"/api/[controller]/[action]")]
public class AccountController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] AccountLogin loginModel)
    {
        // todo 
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "111"),
            new Claim(ClaimTypes.Email, "123456"),
            new Claim(ClaimTypes.Name, "admin"),
        };

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme
            );
        var authProperties = new AuthenticationProperties();
        await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );
        return ApiResult.Success;
    }

    [Authorize]
    [HttpGet]
    public Task<IActionResult> UserProfile()
    {
        var cliams = HttpContext.User.Claims.ToList();
        var userIdClaim = HttpContext.User.Claims
            .First(_ => _.Type == ClaimTypes.NameIdentifier);
        if(userIdClaim == null)
        {
            return Task.FromResult((IActionResult)Unauthorized());
        }

        int userId = Convert.ToInt32(userIdClaim.Value ?? "0");

        // todo 
        var iamIds = new List<int>()
        {
            0, 1,2,3,
        };

        var userName = HttpContext.User.Identity!.Name;
        var userProfile = new UserProfileDto()
        {
            Name = userName!,
            IamIds = iamIds,
        };

        IActionResult res = ApiResult.BuildSuccess(userProfile);
        return Task.FromResult(res);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return ApiResult.Success;
    }
}
