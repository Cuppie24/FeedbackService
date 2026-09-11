using AppController.Controllers.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppController.Controllers;

[ApiController]
[Route("[controller]")]
public class FeedbackController : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateFeedbackAsync()
    {
        throw new NotImplementedException();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetFeedbackAsync(int id)
    {
        throw new NotImplementedException();
    }
}