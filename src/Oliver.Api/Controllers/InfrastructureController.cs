using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Oliver.Api.Controllers
{
    [Route("infrastructure")]
    [ApiController]
    public class InfrastructureController : ControllerBase
    {
        [HttpGet("health")]
        [AllowAnonymous]
        public async Task<IActionResult> HealthCheck() => Ok("Healthy");
    }
}
