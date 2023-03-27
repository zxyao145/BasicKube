using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace BasicKube.Web.Services;

public class IamAuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(claimsPrincipal));
    }

    public void SetAuthInfo(UserProfileDto userProfile)
    {
        var identity = new ClaimsIdentity(new[]{
            new Claim("IamIds", userProfile.GetIamIdStr()),
            new Claim(ClaimTypes.Name, userProfile.Name),
        }, "AuthCookie");

        claimsPrincipal = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void ClearAuthInfo()
    {
        claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
