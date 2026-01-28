using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVoto.Api.Data;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Controllers
{
    [ApiController]
    [Route("api/elecciones/{eleccionId:int}/listas")]
    public class ListasController : ControllerBase
    {
        private readonly SistemaVotoDbContext _db;
        public ListasController(SistemaVotoDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll(int eleccionId)
        {
            var e = await _db.Elecciones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == eleccionId);
            if (e is null) return NotFound(ApiResult<object>.Fail("Elección no existe."));

            var listas = await _db.Listas.AsNoTracking()
                .Where(l => l.EleccionId == eleccionId)
                .OrderBy(l => l.Id)
                .ToListAsync();

            return Ok(ApiResult<List<Lista>>.Ok(listas));
        }

        public record CrearListaRequest(string Nombre, string? LogoUrl);

        [HttpPost]
        public async Task<IActionResult> Crear(int eleccionId, [FromBody] CrearListaRequest req)
        {
            var e = await _db.Elecciones.FirstOrDefaultAsync(x => x.Id == eleccionId);
            if (e is null) return NotFound(ApiResult<object>.Fail("Elección no existe."));

            if (e.Tipo != TipoEleccion.Plancha)
                return BadRequest(ApiResult<object>.Fail("Solo elecciones tipo Plancha admiten Listas."));

            if (string.IsNullOrWhiteSpace(req.Nombre))
                return BadRequest(ApiResult<object>.Fail("Nombre de lista es requerido."));

            var lista = new Lista
            {
                EleccionId = eleccionId,
                Nombre = req.Nombre.Trim(),
                LogoUrl = req.LogoUrl
            };

            _db.Listas.Add(lista);
            await _db.SaveChangesAsync();

            return Ok(ApiResult<Lista>.Ok(lista, "Lista creada."));
        }

        public record UpdateListaRequest(string Nombre, string? LogoUrl);

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int eleccionId, int id, [FromBody] UpdateListaRequest req)
        {
            var lista = await _db.Listas.FirstOrDefaultAsync(l => l.EleccionId == eleccionId && l.Id == id);
            if (lista is null) return NotFound(ApiResult<object>.Fail("Lista no existe."));

            if (string.IsNullOrWhiteSpace(req.Nombre))
                return BadRequest(ApiResult<object>.Fail("Nombre es requerido."));

            lista.Nombre = req.Nombre.Trim();
            lista.LogoUrl = req.LogoUrl;

            await _db.SaveChangesAsync();
            return Ok(ApiResult<Lista>.Ok(lista, "Lista actualizada."));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int eleccionId, int id)
        {
            var lista = await _db.Listas.FirstOrDefaultAsync(l => l.EleccionId == eleccionId && l.Id == id);
            if (lista is null) return NotFound(ApiResult<object>.Fail("Lista no existe."));

            _db.Listas.Remove(lista);
            await _db.SaveChangesAsync();

            return Ok(ApiResult<object>.Ok(new { ok = true }, "Lista eliminada."));
        }
    }
}
