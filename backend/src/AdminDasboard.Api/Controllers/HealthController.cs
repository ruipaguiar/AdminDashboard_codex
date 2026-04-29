using Microsoft.AspNetCore.Mvc;

namespace AdminDasboard.Api.Controllers;

[ApiController]
public sealed class HealthController : ControllerBase
{
    [HttpGet("health", Name = "GetHealth")]
    public ActionResult<HealthResponse> Get()
    {
        return Ok(new HealthResponse("ok", DateTimeOffset.UtcNow));
    }
}
