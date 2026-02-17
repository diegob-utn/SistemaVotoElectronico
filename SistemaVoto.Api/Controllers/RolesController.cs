using Microsoft.AspNetCore.Mvc;
using SistemaVoto.Api.Dtos;

namespace SistemaVoto.Api.Controllers
{
    [ApiController]
    [Route("api/roles")]
    public class RolesController : ControllerBase
    {
        // Controlador desactivado por migración a Identity
        public RolesController()
        {
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Controlador desactivado. Use Identity Roles.");
        }
    }
}