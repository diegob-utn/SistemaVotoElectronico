using Microsoft.AspNetCore.Mvc;
using SistemaVoto.Modelos;

namespace SistemaVoto.Data.Controllers
{
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet("/health")]
        public IActionResult Health()
            => Ok(ApiResult<object>.Ok(new { ok = true, utc = DateTime.UtcNow }, "healthy"));
    }
}