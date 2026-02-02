using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVoto.Api.Dtos;
using SistemaVoto.Data.Data;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Controllers
{
    [ApiController]
    [Route("api/recintos")]
    public class RecintosController : ControllerBase
    {
        private readonly SistemaVotoDbContext _db;

        public RecintosController(SistemaVotoDbContext db) => _db = db;

        // GET /api/recintos?ubicacionId=1
        [HttpGet]
        public async Task<ActionResult<ApiResult<object>>> Get([FromQuery] int? ubicacionId = null)
        {
            var q = _db.Recintos.AsQueryable();
            if (ubicacionId is not null) q = q.Where(r => r.UbicacionId == ubicacionId);

            var items = await q
                .OrderBy(r => r.Nombre)
                .Select(r => new { r.Id, r.Nombre, r.Direccion, r.UbicacionId })
                .ToListAsync();

            return Ok(ApiResult<object>.Ok(items));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResult<object>>> GetById(int id)
        {
            var r = await _db.Recintos
                .Where(x => x.Id == id)
                .Select(x => new { x.Id, x.Nombre, x.Direccion, x.UbicacionId })
                .FirstOrDefaultAsync();

            if (r is null) return NotFound(ApiResult<object>.Fail("Recinto no encontrado."));
            return Ok(ApiResult<object>.Ok(r));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResult<object>>> Create([FromBody] CreateRecintoRequest req)
        {
            if (req.UbicacionId is not null)
            {
                var exists = await _db.Ubicaciones.AnyAsync(u => u.Id == req.UbicacionId);
                if (!exists) return BadRequest(ApiResult<object>.Fail("UbicacionId no existe."));
            }

            var r = new RecintoElectoral
            {
                Nombre = req.Nombre.Trim(),
                Direccion = req.Direccion?.Trim(),
                UbicacionId = req.UbicacionId
            };

            _db.Recintos.Add(r);
            await _db.SaveChangesAsync();

            return Ok(ApiResult<object>.Ok(new { r.Id }));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResult<object>>> Update(int id, [FromBody] UpdateRecintoRequest req)
        {
            var r = await _db.Recintos.FirstOrDefaultAsync(x => x.Id == id);
            if (r is null) return NotFound(ApiResult<object>.Fail("Recinto no encontrado."));

            if (req.UbicacionId is not null)
            {
                var exists = await _db.Ubicaciones.AnyAsync(u => u.Id == req.UbicacionId);
                if (!exists) return BadRequest(ApiResult<object>.Fail("UbicacionId no existe."));
            }

            r.Nombre = req.Nombre.Trim();
            r.Direccion = req.Direccion?.Trim();
            r.UbicacionId = req.UbicacionId;

            await _db.SaveChangesAsync();
            return Ok(ApiResult<object>.Ok(new { ok = true }));
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResult<object>>> Delete(int id)
        {
            var r = await _db.Recintos.FirstOrDefaultAsync(x => x.Id == id);
            if (r is null) return NotFound(ApiResult<object>.Fail("Recinto no encontrado."));

            _db.Recintos.Remove(r);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return BadRequest(ApiResult<object>.Fail("No se puede eliminar: está referenciado por votos/usuarios/historial."));
            }

            return Ok(ApiResult<object>.Ok(new { ok = true }));
        }
    }
}
