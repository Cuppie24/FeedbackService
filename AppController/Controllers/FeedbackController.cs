using Microsoft.AspNetCore.Mvc;

namespace AppController.Controllers;

[ApiController]
[Route("[controller]")]
public class FeedbackController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateFeedbackAsync()
    {
        throw new NotImplementedException();
    }

    [HttpGet]
    public async Task<IActionResult> GetFeedbackAsync(int id)
    {
        throw new NotImplementedException();
    }
}