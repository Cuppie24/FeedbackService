using AppController.Authentication;
using AppController.Authentication.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AppController.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthNController(IOptions<JwtOptions> jwtOptions,
    IAuthNService authNService) : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    public  IActionResult IsAuthenticated()
    {
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Login(AuthRequest request)
    {
        Request.Cookies.TryGetValue(jwtOptions.Value.RefreshCookieName, out var refreshTokenText);
        var tokens = await authNService.AuthenticateAsync(request, refreshTokenText);
        if (tokens is null)
            return Unauthorized();
        RefreshTokenCookies(tokens);
        return Ok();
    }

    [HttpGet("refresh")]
    public async Task<IActionResult> Refresh()
    {
        Request.Cookies.TryGetValue(jwtOptions.Value.RefreshCookieName, out var refreshTokenText);
        if (string.IsNullOrWhiteSpace(refreshTokenText))
            return Unauthorized();
        var tokens = await authNService.RotateRefreshTokens(refreshTokenText);
        if (tokens is null)
            return Unauthorized();
        RefreshTokenCookies(tokens);
        return Ok();
    }

    private void RefreshTokenCookies(AuthNTokens tokens)
    {
        CookieHelper.SetHttpOnlyCookie(jwtOptions.Value.CookieName, tokens.JwtToken, jwtOptions.Value.ExpireMinutes, Response);
        CookieHelper.SetHttpOnlyCookie(jwtOptions.Value.RefreshCookieName, tokens.RefreshToken, jwtOptions.Value.RefreshExpireMinutes, Response);
    }
}