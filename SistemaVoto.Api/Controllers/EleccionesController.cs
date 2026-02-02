using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVoto.Api.Dtos;
using SistemaVoto.Data.Data;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Controllers
{
    [ApiController]
    [Route("api/elecciones")]
    public class EleccionesController : ControllerBase
    {
        private readonly SistemaVotoDbContext _db;

        public EleccionesController(SistemaVotoDbContext db) => _db = db;

        // GET /api/elecciones?page=1&pageSize=20
        [HttpGet]
        public async Task<ActionResult<ApiResult<object>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var total = await _db.Elecciones.CountAsync();

            var items = await _db.Elecciones
                .OrderByDescending(e => e.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EleccionListItemDto
                {
                    Id = e.Id,
                    Titulo = e.Titulo,
                    Descripcion = e.Descripcion,
                    FechaInicioUtc = e.FechaInicioUtc,
                    FechaFinUtc = e.FechaFinUtc,
                    Tipo = e.Tipo,
                    NumEscanos = e.NumEscanos,
                    Estado = e.Estado,
                    UsaUbicacion = e.UsaUbicacion,
                    ModoUbicacion = e.ModoUbicacion
                })
                .ToListAsync();

            return Ok(ApiResult<object>.Ok(new { page, pageSize, total, items }));
        }

        // POST /api/elecciones
        [HttpPost]
        public async Task<ActionResult<ApiResult<object>>> Create([FromBody] CreateEleccionRequest req)
        {
            if (req.FechaFinUtc <= req.FechaInicioUtc)
                return BadRequest(ApiResult<object>.Fail("FechaFinUtc debe ser mayor que FechaInicioUtc."));

            // Coherencia tipo/escaños (además tienes check constraint en DB)
            if (req.Tipo == TipoEleccion.Nominal && req.NumEscanos != 0)
                return BadRequest(ApiResult<object>.Fail("Si Tipo es Nominal, NumEscanos debe ser 0."));
            if (req.Tipo == TipoEleccion.Plancha && req.NumEscanos <= 0)
                return BadRequest(ApiResult<object>.Fail("Si Tipo es Plancha, NumEscanos debe ser > 0."));

            // Ubicación opcional
            if (!req.UsaUbicacion)
            {
                req.ModoUbicacion = ModoUbicacion.Ninguna;
            }
            else
            {
                if (req.ModoUbicacion == ModoUbicacion.Ninguna)
                    return BadRequest(ApiResult<object>.Fail("Si UsaUbicacion=true, ModoUbicacion no puede ser Ninguna."));
            }

            var e = new Eleccion
            {
                Titulo = req.Titulo.Trim(),
                Descripcion = req.Descripcion?.Trim(),
                FechaInicioUtc = req.FechaInicioUtc,
                FechaFinUtc = req.FechaFinUtc,
                Tipo = req.Tipo,
                NumEscanos = req.NumEscanos,
                Estado = EstadoEleccion.Pendiente,
                UsaUbicacion = req.UsaUbicacion,
                ModoUbicacion = req.ModoUbicacion
            };

            _db.Elecciones.Add(e);
            await _db.SaveChangesAsync();

            return Ok(ApiResult<object>.Ok(new { e.Id }));
        }

        // POST /api/elecciones/{id}/ubicaciones
        [HttpPost("{id:int}/ubicaciones")]
        public async Task<ActionResult<ApiResult<object>>> SetUbicaciones(int id, [FromBody] AssignUbicacionesRequest req)
        {
            var eleccion = await _db.Elecciones.FirstOrDefaultAsync(x => x.Id == id);
            if (eleccion is null) return NotFound(ApiResult<object>.Fail("Elección no encontrada."));

            if (!eleccion.UsaUbicacion)
                return BadRequest(ApiResult<object>.Fail("Esta elección no usa ubicación. Activa UsaUbicacion al crearla."));

            var distinctIds = req.UbicacionIds.Distinct().ToList();
            if (distinctIds.Count == 0)
                return BadRequest(ApiResult<object>.Fail("UbicacionIds está vacío."));

            var existentes = await _db.Ubicaciones
                .Where(u => distinctIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();

            if (existentes.Count != distinctIds.Count)
                return BadRequest(ApiResult<object>.Fail("Una o más UbicacionIds no existen."));

            // Reemplazar asignaciones (simple y limpio)
            var actuales = await _db.EleccionUbicaciones.Where(x => x.EleccionId == id).ToListAsync();
            _db.EleccionUbicaciones.RemoveRange(actuales);

            foreach (var uid in distinctIds)
                _db.EleccionUbicaciones.Add(new EleccionUbicacion { EleccionId = id, UbicacionId = uid });

            await _db.SaveChangesAsync();
            return Ok(ApiResult<object>.Ok(new { ok = true, count = distinctIds.Count }));
        }

        // GET /api/elecciones/{id}/ubicaciones
        [HttpGet("{id:int}/ubicaciones")]
        public async Task<ActionResult<ApiResult<object>>> GetUbicaciones(int id)
        {
            var eleccion = await _db.Elecciones.FirstOrDefaultAsync(x => x.Id == id);
            if (eleccion is null) return NotFound(ApiResult<object>.Fail("Elección no encontrada."));

            var items = await _db.EleccionUbicaciones
                .Where(x => x.EleccionId == id)
                .Join(_db.Ubicaciones,
                    eu => eu.UbicacionId,
                    u => u.Id,
                    (eu, u) => new { u.Id, u.Nombre, u.Tipo, u.ParentId })
                .OrderBy(x => x.Tipo).ThenBy(x => x.Nombre)
                .ToListAsync();

            return Ok(ApiResult<object>.Ok(items));
        }


        /*
        // GET /api/elecciones/{id}/conteo?ubicacionId=1&recintoId=2
        // ✅ PLANO (sin ApiResult) para charts/widgets
        [HttpGet("{id:int}/conteo")]
        public async Task<IActionResult> Conteo(
            int id,
            [FromQuery] int? ubicacionId = null,
            [FromQuery] int? recintoId = null)
        {
            var eleccion = await _db.Elecciones.FirstOrDefaultAsync(x => x.Id == id);
            if (eleccion is null) return NotFound(new { error = "Elección no encontrada" });

            // -----------------------------
            // Validación de filtros por modo
            // -----------------------------
            if (!eleccion.UsaUbicacion || eleccion.ModoUbicacion == ModoUbicacion.Ninguna)
            {
                if (ubicacionId is not null || recintoId is not null)
                    return BadRequest(new { error = "Esta elección NO usa ubicación, no acepto filtros ubicacionId/recintoId." });
            }
            else if (eleccion.ModoUbicacion == ModoUbicacion.PorUbicacion)
            {
                if (recintoId is not null)
                    return BadRequest(new { error = "Esta elección filtra por UbicacionId. No acepto recintoId." });

                if (ubicacionId is null)
                {
                    // filtro opcional: permitido sin ubicacionId (trae el total)
                }
                else
                {
                    var exists = await _db.Ubicaciones.AnyAsync(u => u.Id == ubicacionId.Value);
                    if (!exists) return BadRequest(new { error = "UbicacionId no existe." });

                    // Si manejas tabla de asignación (EleccionUbicaciones), valida que esté habilitada:
                    if (_db.EleccionUbicaciones is not null)
                    {
                        var allowed = await _db.EleccionUbicaciones
                            .AnyAsync(eu => eu.EleccionId == id && eu.UbicacionId == ubicacionId.Value);

                        if (!allowed)
                            return BadRequest(new { error = "UbicacionId no está habilitada para esta elección." });
                    }
                }
            }
            else if (eleccion.ModoUbicacion == ModoUbicacion.PorRecinto)
            {
                if (ubicacionId is not null)
                    return BadRequest(new { error = "Esta elección filtra por RecintoId. No acepto ubicacionId." });

                if (recintoId is null)
                {
                    // filtro opcional: permitido sin recintoId (trae el total)
                }
                else
                {
                    var exists = await _db.Recintos.AnyAsync(r => r.Id == recintoId.Value);
                    if (!exists) return BadRequest(new { error = "RecintoId no existe." });
                }
            }

            // -----------------------------
            // Query base de votos (con filtros)
            // -----------------------------
            IQueryable<Voto> votosQ = _db.Votos.Where(v => v.EleccionId == id);

            if (eleccion.UsaUbicacion && eleccion.ModoUbicacion == ModoUbicacion.PorUbicacion && ubicacionId is not null)
                votosQ = votosQ.Where(v => v.UbicacionId == ubicacionId.Value);

            if (eleccion.UsaUbicacion && eleccion.ModoUbicacion == ModoUbicacion.PorRecinto && recintoId is not null)
                votosQ = votosQ.Where(v => v.RecintoId == recintoId.Value);

            // -----------------------------
            // NOMINAL (por candidato)
            // -----------------------------
            if (eleccion.Tipo == TipoEleccion.Nominal)
            {
                var candidatos = await _db.Candidatos
                    .Where(c => c.EleccionId == id)
                    .Select(c => new { c.Id, c.Nombre })
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();

                var conteos = await votosQ
                    .Where(v => v.CandidatoId != null)
                    .GroupBy(v => v.CandidatoId!.Value)
                    .Select(g => new { CandidatoId = g.Key, Count = g.Count() })
                    .ToListAsync();

                var categorias = new List<string>();
                var votos = new List<int>();

                foreach (var c in candidatos)
                {
                    categorias.Add(c.Nombre);
                    votos.Add(conteos.FirstOrDefault(x => x.CandidatoId == c.Id)?.Count ?? 0);
                }

                var totalVotos = votos.Sum();
                var porcentajes = votos.Select(x => totalVotos == 0 ? 0 : Math.Round(x * 100.0 / totalVotos, 2)).ToList();

                return Ok(new
                {
                    categorias,
                    votos,
                    porcentajes,
                    totalVotos
                });
            }

            // -----------------------------
            // PLANCHA (por lista) + escaños
            // -----------------------------
            var listas = await _db.Listas
                .Where(l => l.EleccionId == id)
                .Select(l => new { l.Id, l.Nombre })
                .OrderBy(l => l.Nombre)
                .ToListAsync();

            var conteosListas = await votosQ
                .Where(v => v.ListaId != null)
                .GroupBy(v => v.ListaId!.Value)
                .Select(g => new { ListaId = g.Key, Count = g.Count() })
                .ToListAsync();

            var categorias2 = new List<string>();
            var votos2 = new List<int>();

            foreach (var l in listas)
            {
                categorias2.Add(l.Nombre);
                votos2.Add(conteosListas.FirstOrDefault(x => x.ListaId == l.Id)?.Count ?? 0);
            }

            var totalVotos2 = votos2.Sum();
            var porcentajes2 = votos2.Select(x => totalVotos2 == 0 ? 0 : Math.Round(x * 100.0 / totalVotos2, 2)).ToList();

            var dh = AllocateDhondt(votos2, eleccion.NumEscanos);
            var web = AllocateWebster(votos2, eleccion.NumEscanos);

            return Ok(new
            {
                categorias = categorias2,
                votos = votos2,
                porcentajes = porcentajes2,
                totalVotos = totalVotos2,
                escanosDhondt = dh,
                escanosWebster = web
            });
        }
        */

        // -----------------------------
        // Escaños (D’Hondt y Webster)
        // -----------------------------
        private static List<int> AllocateDhondt(List<int> votes, int seats)
        {
            var n = votes.Count;
            var alloc = Enumerable.Repeat(0, n).ToList();

            for (int s = 0; s < seats; s++)
            {
                var bestIdx = 0;
                double bestScore = -1;

                for (int i = 0; i < n; i++)
                {
                    var score = votes[i] / (double)(alloc[i] + 1);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestIdx = i;
                    }
                }
                alloc[bestIdx]++;
            }

            return alloc;
        }

        private static List<int> AllocateWebster(List<int> votes, int seats)
        {
            var n = votes.Count;
            if (seats <= 0 || votes.Sum() == 0) return Enumerable.Repeat(0, n).ToList();

            double low = 1e-9, high = Math.Max(1, votes.Max());
            List<int> best = Enumerable.Repeat(0, n).ToList();

            for (int it = 0; it < 60; it++)
            {
                var mid = (low + high) / 2.0;
                var alloc = votes.Select(v => (int)Math.Round(v / mid, MidpointRounding.AwayFromZero)).ToList();
                var sum = alloc.Sum();

                if (sum == seats) return alloc;

                if (sum > seats) low = mid;
                else high = mid;

                best = alloc;
            }

            while (best.Sum() > seats)
            {
                var idx = best.IndexOf(best.Max());
                best[idx]--;
            }
            while (best.Sum() < seats)
            {
                var idx = votes.IndexOf(votes.Max());
                best[idx]++;
            }

            return best;
        }

    }
}
