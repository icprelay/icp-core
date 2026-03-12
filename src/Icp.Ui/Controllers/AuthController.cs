using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Icp.Ui;

[Authorize]
[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    [HttpGet("refresh")]
    public IActionResult Refresh([FromQuery] string? returnUrl = null)
    {
        var target = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        return Challenge(new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = target }, OpenIdConnectDefaults.AuthenticationScheme);
    }
}
