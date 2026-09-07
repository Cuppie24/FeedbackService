using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppController.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthNController : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    public  IActionResult IsAuthenticated()
    {
        return Ok();
    }
    
    
}