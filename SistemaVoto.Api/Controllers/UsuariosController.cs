using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SistemaVoto.Api.Dtos;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Controllers
{
    [ApiController]
    [Route("api/usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;

        public UsuariosController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var users = await _userManager.Users
                .Select(u => new { u.Id, u.Email })
                .ToListAsync();
            
            return Ok(ApiResult<object>.Ok(users));
        }
    }
}
