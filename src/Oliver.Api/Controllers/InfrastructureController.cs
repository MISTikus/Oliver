using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Oliver.Api.Controllers;

[Route("infrastructure")]
[ApiController]
public class InfrastructureController : ControllerBase
{
    [HttpGet("health")]
    [AllowAnonymous]
    public ActionResult<string> HealthCheck() => Ok("Healthy");
}
