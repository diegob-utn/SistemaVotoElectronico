using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVoto.Api.Data;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Controllers
{
    [ApiController]
    [Route("api/elecciones/{eleccionId:int}/auditoria")]
    public class AuditoriaController : ControllerBase
    {
        private readonly SistemaVotoDbContext _db;
        public AuditoriaController(SistemaVotoDbContext db) => _db = db;

        [HttpGet("historial")]
        public async Task<IActionResult> Historial(int eleccionId, int page = 1, int pageSize = 50)
        {
            var ok = await _db.Elecciones.AnyAsync(e => e.Id == eleccionId);
            if (!ok) return NotFound(ApiResult<object>.Fail("Elección no existe."));

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var q = _db.HistorialVotaciones.AsNoTracking()
                .Where(h => h.EleccionId == eleccionId)
                .OrderByDescending(h => h.FechaParticipacionUtc);

            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(h => new
                {
                    h.Id,
                    h.UsuarioId,
                    h.FechaParticipacionUtc,
                    h.HashTransaccion
                })
                .ToListAsync();

            return Ok(ApiResult<object>.Ok(new { page, pageSize, total, items }));
        }

        [HttpGet("votos")]
        public async Task<IActionResult> Votos(int eleccionId, int page = 1, int pageSize = 200)
        {
            var ok = await _db.Elecciones.AnyAsync(e => e.Id == eleccionId);
            if (!ok) return NotFound(ApiResult<object>.Fail("Elección no existe."));

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 500);

            var q = _db.Votos.AsNoTracking()
                .Where(v => v.EleccionId == eleccionId)
                .OrderByDescending(v => v.Id);

            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(v => new
                {
                    v.Id,
                    v.EleccionId,
                    v.CandidatoId,
                    v.ListaId,
                    v.FechaVotoUtc,
                    v.HashPrevio,
                    v.HashActual
                })
                .ToListAsync();

            return Ok(ApiResult<object>.Ok(new { page, pageSize, total, items }));
        }
    }
}
