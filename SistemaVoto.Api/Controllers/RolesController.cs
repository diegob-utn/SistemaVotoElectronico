using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVoto.Api.Data;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Controllers
{
    [ApiController]
    [Route("api/roles")]
    public class RolesController : ControllerBase
    {
        private readonly SistemaVotoDbContext _db;
        public RolesController(SistemaVotoDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var roles = await _db.Roles.AsNoTracking().OrderBy(r => r.Id).ToListAsync();
            return Ok(ApiResult<List<Rol>>.Ok(roles));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var rol = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (rol is null) return NotFound(ApiResult<object>.Fail("Rol no existe."));
            return Ok(ApiResult<Rol>.Ok(rol));
        }

        // ✅ MODIFICADO: Agregados Descripcion y Activo (opcionales)
        public record CrearRolRequest(string Nombre, string? Descripcion, bool? Activo);

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearRolRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Nombre))
                return BadRequest(ApiResult<object>.Fail("Nombre de rol es requerido."));

            var nombre = req.Nombre.Trim();
            var exists = await _db.Roles.AnyAsync(r => r.Nombre == nombre);
            if (exists) return Conflict(ApiResult<object>.Fail("Ya existe un rol con ese nombre."));

            // ✅ MODIFICADO: Asignación de los nuevos campos
            var rol = new Rol
            {
                Nombre = nombre,
                Descripcion = req.Descripcion,
                // Si no envían 'Activo', asumimos true por defecto
                Activo = req.Activo ?? true
            };

            _db.Roles.Add(rol);
            await _db.SaveChangesAsync();

            return Ok(ApiResult<Rol>.Ok(rol, "Rol creado."));
        }
    }
}