using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SistemaVoto.Api.Dtos;
using SistemaVoto.Api.Hubs;
using SistemaVoto.Data.Data;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Controllers
{
    [ApiController]
    [Route("api/elecciones/{eleccionId}")]
    public class VotosController : ControllerBase
    {
        private readonly SistemaVotoDbContext _db;
        private readonly IHubContext<VotacionHub> _hubContext;
        private readonly UserManager<IdentityUser> _userManager;

        public VotosController(SistemaVotoDbContext db, IHubContext<VotacionHub> hubContext, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _hubContext = hubContext;
            _userManager = userManager;
        }

        [HttpPost("votar")]
        public async Task<ActionResult<ApiResult<object>>> Votar(int eleccionId, [FromBody] VotarRequest req)
        {
            try 
            {
                var eleccion = await _db.Elecciones
                    .Include(e => e.Candidatos)
                    .Include(e => e.Listas)
                    .FirstOrDefaultAsync(e => e.Id == eleccionId);

                if (eleccion == null)
                    return NotFound(new ApiResult<object> { Success = false, Message = "Elección no encontrada" });

                // 1. Validar estado y fecha
                var now = DateTime.UtcNow;
                if (eleccion.Estado != EstadoEleccion.Activa)
                    return BadRequest(new ApiResult<object> { Success = false, Message = "La elección no está activa" });

                if (now < eleccion.FechaInicioUtc || now > eleccion.FechaFinUtc)
                    return BadRequest(new ApiResult<object> { Success = false, Message = "La elección está fuera del periodo de votación" });

                // 2. VALIDAR USUARIO (Identity - String ID)
                if (string.IsNullOrEmpty(req.UsuarioId))
                    return BadRequest(new ApiResult<object> { Success = false, Message = "UsuarioId es requerido" });

                var user = await _userManager.FindByIdAsync(req.UsuarioId);
                if (user == null)
                {
                    return BadRequest(new ApiResult<object> { Success = false, Message = "Usuario no encontrado" });
                }

                // 3. Verificar si ya votó
                var yaVoto = await _db.HistorialVotaciones
                    .AnyAsync(h => h.EleccionId == eleccionId && h.UsuarioId == req.UsuarioId);

                if (yaVoto)
                    return BadRequest(new ApiResult<object> { Success = false, Message = "El usuario ya ha votado en esta elección" });

                // 4. Validar Ubicación / Recinto (Si aplica)
                int? ubicacionId = null;
                int? recintoId = null;

                if (eleccion.UsaUbicacion)
                {
                    if (eleccion.ModoUbicacion == ModoUbicacion.PorUbicacion)
                    {
                        if (req.UbicacionId == null)
                            return BadRequest(new ApiResult<object> { Success = false, Message = "Esta elección requiere Ubicación" });
                        
                        // Validar existencia
                        var ubiExists = await _db.Ubicaciones.AnyAsync(u => u.Id == req.UbicacionId);
                        if (!ubiExists)
                             return BadRequest(new ApiResult<object> { Success = false, Message = "Ubicación inválida" });
                        
                        ubicacionId = req.UbicacionId;
                    }
                    else if (eleccion.ModoUbicacion == ModoUbicacion.PorRecinto)
                    {
                        if (req.RecintoId == null)
                            return BadRequest(new ApiResult<object> { Success = false, Message = "Esta elección requiere Recinto" });

                        var recExists = await _db.Recintos.AnyAsync(r => r.Id == req.RecintoId);
                        if (!recExists)
                             return BadRequest(new ApiResult<object> { Success = false, Message = "Recinto inválido" });

                        recintoId = req.RecintoId;
                    }
                }

                // 5. Validar Selección (Candidato XOR Lista)
                if (req.CandidatoId.HasValue && req.ListaId.HasValue)
                    return BadRequest(new ApiResult<object> { Success = false, Message = "No puede votar por candidato y lista simultáneamente" });

                if (!req.CandidatoId.HasValue && !req.ListaId.HasValue)
                    return BadRequest(new ApiResult<object> { Success = false, Message = "Debe seleccionar un candidato o una lista" });

                // 6. Validar Pertenencia
                if (req.CandidatoId.HasValue)
                {
                    var cand = eleccion.Candidatos.FirstOrDefault(c => c.Id == req.CandidatoId.Value);
                    if (cand == null)
                        return BadRequest(new ApiResult<object> { Success = false, Message = "Candidato no válido para esta elección" });
                }

                if (req.ListaId.HasValue)
                {
                    var list = eleccion.Listas.FirstOrDefault(l => l.Id == req.ListaId.Value);
                    if (list == null)
                        return BadRequest(new ApiResult<object> { Success = false, Message = "Lista no válida para esta elección" });
                }

                // 7. Hash Chain
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

                // 8. Registrar
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

                _db.Votos.Add(voto);

                var historial = new HistorialVotacion
                {
                    EleccionId = eleccionId,
                    UsuarioId = req.UsuarioId,
                    FechaParticipacionUtc = nowUtc,
                    HashTransaccion = ComputeSha256($"TX|{eleccionId}|{req.UsuarioId}|{nowUtc:O}"),
                    UbicacionId = ubicacionId,
                    RecintoId = recintoId
                };
                _db.HistorialVotaciones.Add(historial);

                await _db.SaveChangesAsync();

                // 9. Notificar SignalR
                await _hubContext.Clients.Group(eleccionId.ToString()).SendAsync("ActualizacionResultados", eleccionId);

                return Ok(new ApiResult<object> { Success = true, Message = "Voto registrado exitosamente", Data = new { votoId = voto.Id } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResult<object> { Success = false, Message = "Error interno: " + ex.Message });
            }
        }

        private static string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
