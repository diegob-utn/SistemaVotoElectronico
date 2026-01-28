using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVoto.Api.Data;
using SistemaVoto.Api.Dtos;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Controllers
{
    [ApiController]
    [Route("api/ubicaciones")]
    public class UbicacionesController : ControllerBase
    {
        private readonly SistemaVotoDbContext _db;

        public UbicacionesController(SistemaVotoDbContext db) => _db = db;

        // GET /api/ubicaciones?parentId=1
        [HttpGet]
        public async Task<ActionResult<ApiResult<object>>> Get([FromQuery] int? parentId = null)
        {
            var items = await _db.Ubicaciones
                .Where(u => u.ParentId == parentId)
                .OrderBy(u => u.Tipo).ThenBy(u => u.Nombre)
                .Select(u => new { u.Id, u.Nombre, u.Tipo, u.ParentId })
                .ToListAsync();

            return Ok(ApiResult<object>.Ok(items));
        }

        // GET /api/ubicaciones/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResult<object>>> GetById(int id)
        {
            var u = await _db.Ubicaciones
                .Where(x => x.Id == id)
                .Select(x => new { x.Id, x.Nombre, x.Tipo, x.ParentId })
                .FirstOrDefaultAsync();

            if (u is null) return NotFound(ApiResult<object>.Fail("Ubicación no encontrada."));
            return Ok(ApiResult<object>.Ok(u));
        }

        // POST /api/ubicaciones
        [HttpPost]
        public async Task<ActionResult<ApiResult<object>>> Create([FromBody] CreateUbicacionRequest req)
        {
            if (req.ParentId is not null)
            {
                var parentExists = await _db.Ubicaciones.AnyAsync(x => x.Id == req.ParentId);
                if (!parentExists) return BadRequest(ApiResult<object>.Fail("ParentId no existe."));
            }

            var u = new Ubicacion
            {
                Nombre = req.Nombre.Trim(),
                Tipo = req.Tipo.Trim(),
                ParentId = req.ParentId
            };

            _db.Ubicaciones.Add(u);
            await _db.SaveChangesAsync();

            return Ok(ApiResult<object>.Ok(new { u.Id }));
        }

        // PUT /api/ubicaciones/1
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResult<object>>> Update(int id, [FromBody] UpdateUbicacionRequest req)
        {
            var u = await _db.Ubicaciones.FirstOrDefaultAsync(x => x.Id == id);
            if (u is null) return NotFound(ApiResult<object>.Fail("Ubicación no encontrada."));

            if (req.ParentId == id) return BadRequest(ApiResult<object>.Fail("ParentId no puede ser el mismo Id."));
            if (req.ParentId is not null)
            {
                var parentExists = await _db.Ubicaciones.AnyAsync(x => x.Id == req.ParentId);
                if (!parentExists) return BadRequest(ApiResult<object>.Fail("ParentId no existe."));
            }

            u.Nombre = req.Nombre.Trim();
            u.Tipo = req.Tipo.Trim();
            u.ParentId = req.ParentId;

            await _db.SaveChangesAsync();
            return Ok(ApiResult<object>.Ok(new { ok = true }));
        }

        // DELETE /api/ubicaciones/1
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResult<object>>> Delete(int id)
        {
            var u = await _db.Ubicaciones.FirstOrDefaultAsync(x => x.Id == id);
            if (u is null) return NotFound(ApiResult<object>.Fail("Ubicación no encontrada."));

            _db.Ubicaciones.Remove(u);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return BadRequest(ApiResult<object>.Fail("No se puede eliminar: tiene hijos o está referenciada por recintos/elecciones/votos."));
            }

            return Ok(ApiResult<object>.Ok(new { ok = true }));
        }
    }
}
