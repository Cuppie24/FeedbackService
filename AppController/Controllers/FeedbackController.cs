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

    [HttpGet("can-edit/{id:int}")]
    [Authorize]
    public async Task<ActionResult<SimpleResponse<bool>>> CanEditFeedbackAsync(int id)
    {
        throw new NotImplementedException();
        return new SimpleResponse<bool>(false, "You can only edit feedback info within first 5 minutes");
    }
}