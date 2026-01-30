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

        //  DTO Actualizado: Incluye Descripcion y Activo
        public record CrearRolRequest(string Nombre, string? Descripcion, bool? Activo);

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearRolRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Nombre))
                return BadRequest(ApiResult<object>.Fail("Nombre de rol es requerido."));

            var nombre = req.Nombre.Trim();
            var exists = await _db.Roles.AnyAsync(r => r.Nombre == nombre);
            if (exists) return Conflict(ApiResult<object>.Fail("Ya existe un rol con ese nombre."));

            //  Mapeo de los nuevos campos
            var rol = new Rol
            {
                Nombre = nombre,
                Descripcion = req.Descripcion,
                Activo = req.Activo ?? true // Por defecto 'true' al crear
            };

            _db.Roles.Add(rol);
            await _db.SaveChangesAsync();

            return Ok(ApiResult<Rol>.Ok(rol, "Rol creado."));
        }

        // Nuevo Endpoint: PUT para editar roles
        public record UpdateRolRequest(string Nombre, string? Descripcion, bool? Activo);

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Editar(int id, [FromBody] UpdateRolRequest req)
        {
            var rol = await _db.Roles.FindAsync(id);
            if (rol is null) return NotFound(ApiResult<object>.Fail("Rol no encontrado."));

            // Si se envía nombre, validamos duplicados (excepto si es el mismo)
            if (!string.IsNullOrWhiteSpace(req.Nombre))
            {
                var nombre = req.Nombre.Trim();
                if (nombre != rol.Nombre)
                {
                    var exists = await _db.Roles.AnyAsync(r => r.Nombre == nombre);
                    if (exists) return Conflict(ApiResult<object>.Fail("Ya existe otro rol con ese nombre."));
                    rol.Nombre = nombre;
                }
            }

            // Actualizamos campos opcionales si vienen en el JSON
            if (req.Descripcion != null) rol.Descripcion = req.Descripcion;
            if (req.Activo.HasValue) rol.Activo = req.Activo.Value;

            await _db.SaveChangesAsync();
            return Ok(ApiResult<Rol>.Ok(rol, "Rol actualizado."));
        }
    }
}