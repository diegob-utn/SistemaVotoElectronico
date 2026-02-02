using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVoto.Data.Data;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly SistemaVotoDbContext _db;
    public UsuariosController(SistemaVotoDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.Usuarios.AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new { u.Id, u.NombreCompleto, u.Email, u.RolId })
            .ToListAsync();

        return Ok(ApiResult<object>.Ok(users));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var u = await _db.Usuarios.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.NombreCompleto, x.Email, x.RolId })
            .FirstOrDefaultAsync();

        if (u is null) return NotFound(ApiResult<object>.Fail("Usuario no existe."));
        return Ok(ApiResult<object>.Ok(u));
    }

    public record CrearUsuarioRequest(string NombreCompleto, string Email, string PasswordHash, int RolId);

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearUsuarioRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NombreCompleto))
            return BadRequest(ApiResult<object>.Fail("NombreCompleto es requerido."));
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(ApiResult<object>.Fail("Email es requerido."));
        if (string.IsNullOrWhiteSpace(req.PasswordHash))
            return BadRequest(ApiResult<object>.Fail("PasswordHash es requerido."));

        var email = req.Email.Trim().ToLowerInvariant();

        var rolOk = await _db.Roles.AnyAsync(r => r.Id == req.RolId);
        if (!rolOk) return BadRequest(ApiResult<object>.Fail("RolId no existe."));

        var exists = await _db.Usuarios.AnyAsync(u => u.Email.ToLower() == email);
        if (exists) return Conflict(ApiResult<object>.Fail("Ya existe un usuario con ese email."));

        var user = new Usuario
        {
            NombreCompleto = req.NombreCompleto.Trim(),
            Email = email,
            PasswordHash = req.PasswordHash,
            RolId = req.RolId
        };

        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        return Ok(ApiResult<object>.Ok(new { user.Id, user.NombreCompleto, user.Email, user.RolId }, "Usuario creado."));
    }

    public record CambiarRolRequest(int RolId);

    [HttpPut("{id:int}/rol")]
    public async Task<IActionResult> CambiarRol(int id, [FromBody] CambiarRolRequest req)
    {
        var user = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound(ApiResult<object>.Fail("Usuario no existe."));

        var rolOk = await _db.Roles.AnyAsync(r => r.Id == req.RolId);
        if (!rolOk) return BadRequest(ApiResult<object>.Fail("RolId no existe."));

        user.RolId = req.RolId;
        await _db.SaveChangesAsync();

        return Ok(ApiResult<object>.Ok(new { user.Id, user.RolId }, "Rol actualizado."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound(ApiResult<object>.Fail("Usuario no existe."));

        _db.Usuarios.Remove(user);
        await _db.SaveChangesAsync();

        return Ok(ApiResult<object>.Ok(new { ok = true }, "Usuario eliminado."));
    }
}
