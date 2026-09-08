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
        // var token = await authNService.AuthenticateAsync(request);
        // if (string.IsNullOrWhiteSpace(token))
        //     return StatusCode(StatusCodes.Status500InternalServerError);
        // Response.Cookies.Append(jwtOptions.Value.CookieName, token, new CookieOptions
        // {
        //     HttpOnly = true,
        //     Secure = true,
        //     SameSite = SameSiteMode.Lax,
        //     Expires = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.Value.ExpireMinutes)
        // });
        // return Ok(new {Token = token});
    }
}