using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SistemaVoto.Data.Data;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _config;

        public AuthController(UserManager<IdentityUser> userManager, IConfiguration config)
        {
            _userManager = userManager;
            _config = config;
        }

        public record LoginRequest(string Email, string Password);
        public record RegisterRequest(string Email, string Password, string NombreCompleto);

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(ApiResult<object>.Fail("Email y contraseña requeridos."));

            // Verificar si existe
            var existingUser = await _userManager.FindByEmailAsync(req.Email);
            if (existingUser != null)
                return BadRequest(ApiResult<object>.Fail("El usuario ya existe."));

            var user = new IdentityUser { UserName = req.Email, Email = req.Email };
            var result = await _userManager.CreateAsync(user, req.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Usuario");
                
                // Retornar objeto similar a login para uso inmediato si se requiere
                return Ok(ApiResult<object>.Ok(new { 
                    Id = user.Id, 
                    Email = user.Email,
                    Message = "Usuario creado exitosamente"
                }));
            }

            return BadRequest(ApiResult<object>.Fail(string.Join(", ", result.Errors.Select(e => e.Description))));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(ApiResult<object>.Fail("Email y contraseña requeridos."));

            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null)
                return Unauthorized(ApiResult<object>.Fail("Credenciales inválidas."));

            var result = await _userManager.CheckPasswordAsync(user, req.Password);
            if (!result)
                return Unauthorized(ApiResult<object>.Fail("Credenciales inválidas."));

            // Generar Token
            var keyStr = _config["Jwt:Key"] ?? "SecretKey_SuperSegura_Para_Desarrollo_123456";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var roles = await _userManager.GetRolesAsync(user);
            
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("id", user.Id), // IdentityUser Id is string
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "SistemaVotoApi",
                audience: _config["Jwt:Audience"] ?? "SistemaVotoClient",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(4),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(ApiResult<object>.Ok(new
            {
                token = jwt,
                user = new { Id = user.Id, NombreCompleto = user.UserName, Email = user.Email, Roles = roles }
            }));
        }
    }
}
