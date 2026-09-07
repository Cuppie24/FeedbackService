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
        var token = await authNService.AuthenticateAsync(request);
        if (string.IsNullOrWhiteSpace(token))
            return StatusCode(StatusCodes.Status500InternalServerError);
        _ = Request.Cookies.Append(new KeyValuePair<string, string>(jwtOptions.Value.CookieName, token));
        return Ok(new {Token = token});
    }
}