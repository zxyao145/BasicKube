using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace BasicKube.Api.Controllers.Account;

public class UserIamInfo
{
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public List<IamNodeInfo> IamIds { get; set; } = new();
}

/// <summary>
/// 应该集成外部的接口
/// </summary>
[ApiController]
[Route($"/api/[controller]/[action]")]
[Authorize]
public class AccountController : ControllerBase
{
    static List<UserIamInfo> MockUserInfo = new List<UserIamInfo>()
    {
        new UserIamInfo
        {
            UserId = 1,
            UserName = "zxc",
            IamIds = new List<IamNodeInfo>()
            {
                new IamNodeInfo
                {
                    IamId = 0,
                    Project = "Project-1"
                },
                new IamNodeInfo
                {
                    IamId = 2,
                    Project = "Project-2"
                }
            }
        },
        new UserIamInfo
        {
            UserId = 2,
            UserName = "asd",
            IamIds = new List<IamNodeInfo>()
            {
                new IamNodeInfo
                {
                    IamId = 1,
                    Project = "Project-1"
                },
                new IamNodeInfo
                {
                    IamId = 3,
                    Project = "Project-3"
                },
                new IamNodeInfo
                {
                    IamId = 4,
                    Project = "Project-4"
                }
            }
        }
    };

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] AccountLogin loginModel)
    {
        var user = MockUserInfo.FirstOrDefault(x => x.UserName == loginModel.Name);
        if (user == null)
        {
            ModelState.AddModelError("user", "user not existed");
            return BadRequest(ModelState);
        }
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId + ""),
            new Claim(ClaimTypes.Name, user.UserName),
        };

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme
            );
        await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity)
            );
        return ApiResult.Success;
    }


    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return ApiResult.Success;
    }

    [HttpGet]
    public Task<IActionResult> UserProfile()
    {
        var cliams = HttpContext.User.Claims.ToList();
        var userIdClaim = HttpContext.User.Claims
            .First(_ => _.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Task.FromResult((IActionResult)Unauthorized());
        }
        int userId = Convert.ToInt32(userIdClaim.Value ?? "0");
        var user = MockUserInfo.FirstOrDefault(x => x.UserId == userId);
        if (user == null)
        {
            return Task.FromResult((IActionResult)Unauthorized());
        }

        var iamIds = user.IamIds.Select(x => x.IamId).ToList();
        var userProfile = new UserProfileDto()
        {
            Name = user.UserName,
            IamIds = iamIds,
        };

        IActionResult res = ApiResult.BuildSuccess(userProfile);
        return Task.FromResult(res);
    }

    [HttpGet]
    public Task<IActionResult> UserIamInfo()
    {
        var cliams = HttpContext.User.Claims.ToList();
        var userIdClaim = HttpContext.User.Claims
            .First(_ => _.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Task.FromResult((IActionResult)Unauthorized());
        }
        int userId = Convert.ToInt32(userIdClaim.Value ?? "0");
        var user = MockUserInfo.FirstOrDefault(x => x.UserId == userId);
        if (user == null)
        {
            return Task.FromResult((IActionResult)Unauthorized());
        }

        var iamNodes = user.IamIds;
        IActionResult res = ApiResult.BuildSuccess(iamNodes);
        return Task.FromResult(res);
    }
}
