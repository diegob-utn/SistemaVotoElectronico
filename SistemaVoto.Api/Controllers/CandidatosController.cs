using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
//using SistemaVoto.Api.Data;
using SistemaVoto.Data.Data;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Controllers
{
    [ApiController]
    [Route("api/elecciones/{eleccionId:int}/candidatos")]
    public class CandidatosController : ControllerBase
    {
        private readonly SistemaVotoDbContext _db;
        public CandidatosController(SistemaVotoDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll(int eleccionId)
        {
            var ok = await _db.Elecciones.AnyAsync(e => e.Id == eleccionId);
            if (!ok) return NotFound(ApiResult<object>.Fail("Elección no existe."));

            var candidatos = await _db.Candidatos.AsNoTracking()
                .Where(c => c.EleccionId == eleccionId)
                .OrderBy(c => c.Id)
                .ToListAsync();

            return Ok(ApiResult<List<Candidato>>.Ok(candidatos));
        }

        public record CrearCandidatoRequest(
            string Nombre,
            string? PartidoPolitico,
            string? FotoUrl,
            string? Propuestas,
            int? ListaId
        );

        [HttpPost]
        public async Task<IActionResult> Crear(int eleccionId, [FromBody] CrearCandidatoRequest req)
        {
            var eleccion = await _db.Elecciones.FirstOrDefaultAsync(e => e.Id == eleccionId);
            if (eleccion is null) return NotFound(ApiResult<object>.Fail("Elección no existe."));

            if (string.IsNullOrWhiteSpace(req.Nombre))
                return BadRequest(ApiResult<object>.Fail("Nombre es requerido."));

            if (eleccion.Tipo == TipoEleccion.Plancha)
            {
                if (req.ListaId is null)
                    return BadRequest(ApiResult<object>.Fail("Plancha requiere ListaId."));
                var listaOk = await _db.Listas.AnyAsync(l => l.Id == req.ListaId && l.EleccionId == eleccionId);
                if (!listaOk) return BadRequest(ApiResult<object>.Fail("ListaId no pertenece a esta elección."));
            }
            else
            {
                if (req.ListaId is not null)
                    return BadRequest(ApiResult<object>.Fail("Nominal no admite ListaId."));
            }

            var c = new Candidato
            {
                EleccionId = eleccionId,
                Nombre = req.Nombre.Trim(),
                PartidoPolitico = req.PartidoPolitico,
                FotoUrl = req.FotoUrl,
                Propuestas = req.Propuestas,
                ListaId = req.ListaId
            };

            _db.Candidatos.Add(c);
            await _db.SaveChangesAsync();

            return Ok(ApiResult<Candidato>.Ok(c, "Candidato creado."));
        }

        public record UpdateCandidatoRequest(
            string Nombre,
            string? PartidoPolitico,
            string? FotoUrl,
            string? Propuestas,
            int? ListaId
        );

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int eleccionId, int id, [FromBody] UpdateCandidatoRequest req)
        {
            var eleccion = await _db.Elecciones.FirstOrDefaultAsync(e => e.Id == eleccionId);
            if (eleccion is null) return NotFound(ApiResult<object>.Fail("Elección no existe."));

            var cand = await _db.Candidatos.FirstOrDefaultAsync(c => c.EleccionId == eleccionId && c.Id == id);
            if (cand is null) return NotFound(ApiResult<object>.Fail("Candidato no existe."));

            if (string.IsNullOrWhiteSpace(req.Nombre))
                return BadRequest(ApiResult<object>.Fail("Nombre es requerido."));

            if (eleccion.Tipo == TipoEleccion.Plancha)
            {
                if (req.ListaId is null)
                    return BadRequest(ApiResult<object>.Fail("Plancha requiere ListaId."));
                var listaOk = await _db.Listas.AnyAsync(l => l.Id == req.ListaId && l.EleccionId == eleccionId);
                if (!listaOk) return BadRequest(ApiResult<object>.Fail("ListaId no pertenece a esta elección."));
            }
            else
            {
                if (req.ListaId is not null)
                    return BadRequest(ApiResult<object>.Fail("Nominal no admite ListaId."));
            }

            cand.Nombre = req.Nombre.Trim();
            cand.PartidoPolitico = req.PartidoPolitico;
            cand.FotoUrl = req.FotoUrl;
            cand.Propuestas = req.Propuestas;
            cand.ListaId = req.ListaId;

            await _db.SaveChangesAsync();
            return Ok(ApiResult<Candidato>.Ok(cand, "Candidato actualizado."));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int eleccionId, int id)
        {
            var cand = await _db.Candidatos.FirstOrDefaultAsync(c => c.EleccionId == eleccionId && c.Id == id);
            if (cand is null) return NotFound(ApiResult<object>.Fail("Candidato no existe."));

            _db.Candidatos.Remove(cand);
            await _db.SaveChangesAsync();

            return Ok(ApiResult<object>.Ok(new { ok = true }, "Candidato eliminado."));
        }
    }
}
