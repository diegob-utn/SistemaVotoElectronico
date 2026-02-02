using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SistemaVoto.Api.Dtos;
using SistemaVoto.Api.Hubs;
using SistemaVoto.Data.Data;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Controllers
{
    [ApiController]
    [Route("api/elecciones/{eleccionId:int}")]
    public class VotosController : ControllerBase
    {
        private readonly SistemaVotoDbContext _db;
        private readonly IHubContext<VotacionHub> _hub;

        public VotosController(SistemaVotoDbContext db, IHubContext<VotacionHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        // POST /api/elecciones/{eleccionId}/votar
        [HttpPost("votar")]
        public async Task<ActionResult<ApiResult<object>>> Votar(int eleccionId, [FromBody] VotarRequest req)
        {
            // 0) Elección existe
            var eleccion = await _db.Elecciones.FirstOrDefaultAsync(e => e.Id == eleccionId);
            if (eleccion is null)
                return NotFound(ApiResult<object>.Fail("Elección no encontrada."));

            // 0.1) (opcional) validar estado/fechas si quieres bloquear fuera de rango:
            // if (eleccion.Estado != EstadoEleccion.Activa) return BadRequest(ApiResult<object>.Fail("Elección no activa."));

            // 1) XOR: exactamente uno (CandidatoId o ListaId)
            var hasCand = req.CandidatoId is not null;
            var hasLista = req.ListaId is not null;
            if (hasCand == hasLista)
                return BadRequest(ApiResult<object>.Fail("Debes enviar exactamente uno: CandidatoId o ListaId."));

            // 2) Validar según tipo de elección
            if (eleccion.Tipo == TipoEleccion.Nominal && !hasCand)
                return BadRequest(ApiResult<object>.Fail("Esta elección es Nominal: requiere CandidatoId."));

            if (eleccion.Tipo == TipoEleccion.Plancha && !hasLista)
                return BadRequest(ApiResult<object>.Fail("Esta elección es Plancha: requiere ListaId."));

            // 3) Anti doble voto (usuario solo 1 voto por elección)
            var yaVoto = await _db.HistorialVotaciones
                .AnyAsync(h => h.EleccionId == eleccionId && h.UsuarioId == req.UsuarioId);

            if (yaVoto)
                return BadRequest(ApiResult<object>.Fail("Este usuario ya votó en esta elección."));

            // 4) Validar pertenencia de candidato/lista a la elección
            if (req.CandidatoId is not null)
            {
                var ok = await _db.Candidatos.AnyAsync(c => c.Id == req.CandidatoId && c.EleccionId == eleccionId);
                if (!ok)
                    return BadRequest(ApiResult<object>.Fail("CandidatoId no pertenece a esta elección."));
            }

            if (req.ListaId is not null)
            {
                var ok = await _db.Listas.AnyAsync(l => l.Id == req.ListaId && l.EleccionId == eleccionId);
                if (!ok)
                    return BadRequest(ApiResult<object>.Fail("ListaId no pertenece a esta elección."));
            }

            // 5) Ubicación opcional (solo exigir si la elección lo pide)
            int? ubicacionId = null;
            int? recintoId = null;

            if (!eleccion.UsaUbicacion || eleccion.ModoUbicacion == ModoUbicacion.Ninguna)
            {
                // Ignorar si el cliente mandó algo
                ubicacionId = null;
                recintoId = null;
            }
            else if (eleccion.ModoUbicacion == ModoUbicacion.PorUbicacion)
            {
                if (req.UbicacionId is null)
                    return BadRequest(ApiResult<object>.Fail("Esta elección requiere UbicacionId."));

                var exists = await _db.Ubicaciones.AnyAsync(u => u.Id == req.UbicacionId);
                if (!exists)
                    return BadRequest(ApiResult<object>.Fail("UbicacionId no existe."));

                // (opcional) si asignas ubicaciones permitidas a la elección, valida:
                // var allowed = await _db.EleccionUbicaciones.AnyAsync(x => x.EleccionId == eleccionId && x.UbicacionId == req.UbicacionId);
                // if (!allowed) return BadRequest(ApiResult<object>.Fail("UbicacionId no está habilitada para esta elección."));

                ubicacionId = req.UbicacionId;
                recintoId = null;
            }
            else if (eleccion.ModoUbicacion == ModoUbicacion.PorRecinto)
            {
                if (req.RecintoId is null)
                    return BadRequest(ApiResult<object>.Fail("Esta elección requiere RecintoId."));

                var exists = await _db.Recintos.AnyAsync(r => r.Id == req.RecintoId);
                if (!exists)
                    return BadRequest(ApiResult<object>.Fail("RecintoId no existe."));

                recintoId = req.RecintoId;
                ubicacionId = null;
            }

            // 6) Hash chain (tamper-evident)
            var lastHash = await _db.Votos
                .Where(v => v.EleccionId == eleccionId)
                .OrderByDescending(v => v.Id)
                .Select(v => v.HashActual)
                .FirstOrDefaultAsync();

            var prevHash = lastHash ?? "GENESIS";
            var nowUtc = DateTime.UtcNow;

            var hashActual = ComputeSha256(
                $"{prevHash}|{eleccionId}|{req.CandidatoId}|{req.ListaId}|{ubicacionId}|{recintoId}|{nowUtc:O}"
            );

            var voto = new Voto
            {
                EleccionId = eleccionId,
                CandidatoId = req.CandidatoId,
                ListaId = req.ListaId,
                FechaVotoUtc = nowUtc,
                HashPrevio = prevHash,
                HashActual = hashActual,
                UbicacionId = ubicacionId,
                RecintoId = recintoId
            };

            var historial = new HistorialVotacion
            {
                EleccionId = eleccionId,
                UsuarioId = req.UsuarioId,
                FechaParticipacionUtc = nowUtc,
                HashTransaccion = ComputeSha256($"TX|{eleccionId}|{req.UsuarioId}|{nowUtc:O}"),
                UbicacionId = ubicacionId,
                RecintoId = recintoId
            };

            _db.Votos.Add(voto);
            _db.HistorialVotaciones.Add(historial);

            await _db.SaveChangesAsync();

            // 7) Notificar SOLO a los dashboards conectados a esa elección
            await _hub.Clients.Group($"eleccion-{eleccionId}")
                .SendAsync("ActualizacionResultados", eleccionId);

            return Ok(ApiResult<object>.Ok(new { ok = true, votoId = voto.Id }));
        }

        private static string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
