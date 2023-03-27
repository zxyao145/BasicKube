using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace BasicKube;

public class IamPage : ComponentBase
{
    [Inject]
    public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Parameter]
    public int IamId { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        var authState = await AuthenticationStateProvider
            .GetAuthenticationStateAsync();
        var user = authState.User;

        //if (user.Identity is not null && user.Identity.IsAuthenticated)
        //{
        //    authMessage = $"{user.Identity.Name} is authenticated.";
        //    claims = user.Claims;
        //    surname = user.FindFirst(c => c.Type == ClaimTypes.Surname)?.Value;
        //}
        //else
        //{
        //    authMessage = "The user is NOT authenticated.";
        //}
    }
}
