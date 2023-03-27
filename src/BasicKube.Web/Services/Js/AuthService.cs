using Blazored.LocalStorage;

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

    public AuthService(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
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
}
