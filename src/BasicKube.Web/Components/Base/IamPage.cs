using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace BasicKube;

public class IamPage : ComponentBase
{
    [Parameter]
    public int IamId { get; set; }
}
