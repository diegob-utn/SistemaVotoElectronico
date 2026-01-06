using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SistemaVoto.Api.Data;
using SistemaVoto.Api.Hubs;
using SistemaVoto.Api.Services;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Controllers
{
    [ApiController]
    [Route("api/elecciones/{eleccionId:int}")]
    public class VotosController : ControllerBase
    {
        private readonly SistemaVotoDbContext _db;
        private readonly VoteHashService _hash;
        private readonly IHubContext<VotacionHub> _hub;

        public VotosController(SistemaVotoDbContext db, VoteHashService hash, IHubContext<VotacionHub> hub)
        {
            _db = db;
            _hash = hash;
            _hub = hub;
        }

        public record VotarRequest(int UsuarioId, int? CandidatoId, int? ListaId);

        [HttpPost("votar")]
        public async Task<IActionResult> Votar(int eleccionId, [FromBody] VotarRequest req)
        {
            var now = DateTime.UtcNow;

            var eleccion = await _db.Elecciones.FirstOrDefaultAsync(e => e.Id == eleccionId);
            if (eleccion is null) return NotFound(ApiResult<object>.Fail("Elección no existe."));

            if (eleccion.Estado != EstadoEleccion.Activa)
                return BadRequest(ApiResult<object>.Fail("Elección no está activa."));

            if (now < eleccion.FechaInicioUtc || now > eleccion.FechaFinUtc)
                return BadRequest(ApiResult<object>.Fail("Fuera de la ventana de votación."));

            if (req.UsuarioId <= 0)
                return BadRequest(ApiResult<object>.Fail("UsuarioId inválido."));

            var ambosNull = req.CandidatoId is null && req.ListaId is null;
            var ambosSet = req.CandidatoId is not null && req.ListaId is not null;
            if (ambosNull || ambosSet)
                return BadRequest(ApiResult<object>.Fail("Debe votar por candidato (nominal) o por lista (plancha), no ambos."));

            if (eleccion.Tipo == TipoEleccion.Nominal)
            {
                if (req.CandidatoId is null) return BadRequest(ApiResult<object>.Fail("Elección nominal requiere CandidatoId."));

                var candOk = await _db.Candidatos.AnyAsync(c => c.Id == req.CandidatoId && c.EleccionId == eleccionId);
                if (!candOk) return BadRequest(ApiResult<object>.Fail("CandidatoId no pertenece a esta elección."));
            }
            else
            {
                if (req.ListaId is null) return BadRequest(ApiResult<object>.Fail("Elección plancha requiere ListaId."));

                var listaOk = await _db.Listas.AnyAsync(l => l.Id == req.ListaId && l.EleccionId == eleccionId);
                if (!listaOk) return BadRequest(ApiResult<object>.Fail("ListaId no pertenece a esta elección."));
            }

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                // PADRÓN (anti doble voto)
                var hist = new HistorialVotacion
                {
                    EleccionId = eleccionId,
                    UsuarioId = req.UsuarioId,
                    FechaParticipacionUtc = now,
                    HashTransaccion = _hash.HashTransaccion(eleccionId, req.UsuarioId, now)
                };
                _db.HistorialVotaciones.Add(hist);

                // URNA (hash encadenado)
                var lastHash = await _db.Votos
                    .Where(v => v.EleccionId == eleccionId)
                    .OrderByDescending(v => v.Id)
                    .Select(v => v.HashActual)
                    .FirstOrDefaultAsync() ?? "GENESIS";

                var voto = new Voto
                {
                    EleccionId = eleccionId,
                    CandidatoId = req.CandidatoId,
                    ListaId = req.ListaId,
                    FechaVotoUtc = now,
                    HashPrevio = lastHash
                };
                voto.HashActual = _hash.HashVote(voto.HashPrevio, eleccionId, voto.CandidatoId, voto.ListaId, voto.FechaVotoUtc);

                _db.Votos.Add(voto);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                await _hub.Clients.Group($"eleccion-{eleccionId}")
                    .SendAsync("ActualizacionResultados", eleccionId);

                return Ok(ApiResult<object>.Ok(new { ok = true }, "Voto registrado."));
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                await tx.RollbackAsync();
                return Conflict(ApiResult<object>.Fail("Ya votó en esta elección."));
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
            => ex.InnerException is PostgresException pg && pg.SqlState == "23505";
    }
}
