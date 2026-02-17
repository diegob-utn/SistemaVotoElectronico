using Microsoft.AspNetCore.Mvc;
using SistemaVoto.Api.Dtos;

namespace SistemaVoto.Api.Controllers
{
    [ApiController]
    [Route("api/usuarios")]
    public class UsuariosController : ControllerBase
    {
        // Controlador desactivado por migración a Identity
        public UsuariosController()
        {
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Controlador desactivado. Use Identity Users.");
        }
    }
}
