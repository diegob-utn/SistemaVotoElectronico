using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaVoto.Data.Data;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly SistemaVotoDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(SistemaVotoDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public record LoginRequest(string Email, string Password);

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(ApiResult<object>.Fail("Email y contraseña requeridos."));

            var user = await _db.Usuarios.AsNoTracking()
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Email == req.Email);

            if (user is null || !user.Activo)
                return Unauthorized(ApiResult<object>.Fail("Credenciales inválidas o usuario inactivo."));

            // TODO: En producción usar hashing real (BCrypt/Argon2). Aquí comparamos directo por simplicidad/demo.
            if (user.PasswordHash != req.Password)
                return Unauthorized(ApiResult<object>.Fail("Credenciales inválidas."));

            // Generar Token
            var keyStr = _config["Jwt:Key"] ?? "SecretKey_SuperSegura_Para_Desarrollo_123456";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("id", user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Rol?.Nombre ?? "Usuario")
            };

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
                user = new { user.Id, user.NombreCompleto, user.Email, Role = user.Rol?.Nombre }
            }));
        }
    }
}
