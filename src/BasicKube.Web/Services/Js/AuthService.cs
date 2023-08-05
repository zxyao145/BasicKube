using BasicKube.Web.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace BasicKube.Web.Common;


public static class AuthServiceExt
{
    public static IServiceCollection AddAuthService(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        return services;
    }
}


public class AuthService
{
    private readonly ILocalStorageService _localStorageService;
    private readonly AuthenticationStateProvider _authStateProvider;

    public AuthService(
        ILocalStorageService localStorageService,
        AuthenticationStateProvider authStateProvider
        )
    {
        _localStorageService = localStorageService;
        _authStateProvider = authStateProvider;
    }

    public async Task SetAuthed()
    {
        await _localStorageService.SetItemAsStringAsync("isauthenticated", "true");
    }

    public async Task<bool> GetIsAuthed()
    {
        var str = await _localStorageService.GetItemAsStringAsync("isauthenticated");
        if (string.IsNullOrEmpty(str))
        {
            return false;
        }
        return str == "true";
    }

    public async Task ClearAuthed()
    {
        await _localStorageService.RemoveItemAsync("isauthenticated");
    }
    #region authStateProvider

    public async Task<bool> HasIamPermission(int iamId)
    {
        var claimsPrincipal = (await _authStateProvider.GetAuthenticationStateAsync()).User;

        var claim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == "IamIds");
        var hasPermission = claim != null
            && UserProfileDto.GetIamIdList(claim.Value).Contains(iamId);

        return hasPermission;
    }

    public async Task Logout()
    {
        (_authStateProvider as IamAuthStateProvider)!.ClearAuthInfo();
        await ClearAuthed();
    }

    public async Task<bool> IsAuthenticated()
    {
        var user = (await (_authStateProvider as IamAuthStateProvider)!.GetAuthenticationStateAsync()).User;
        return user?.Identity?.IsAuthenticated ?? false;
    }

    public void SetUserProfile(UserProfileDto userProfile)
    {
        (_authStateProvider as IamAuthStateProvider)!.SetAuthInfo(userProfile);
    }
    #endregion
}
