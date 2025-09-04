using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ShopApplication.Api.Controllers;

[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
    [Authorize]
    [HttpGet("whoami")]
    public IActionResult WhoAmI()
    {
        return Ok(new {
            user = User.Identity?.Name,
            authenticated = User.Identity?.IsAuthenticated ?? false,
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
}
