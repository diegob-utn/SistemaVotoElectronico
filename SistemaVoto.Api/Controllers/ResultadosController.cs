using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVoto.Api.Data;
using SistemaVoto.Api.Services;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Controllers
{
    [ApiController]
    [Route("api/elecciones")]
    public class ResultadosController : ControllerBase
    {
        private readonly SistemaVotoDbContext _db;
        private readonly SeatAllocationService _seats;

        public ResultadosController(SistemaVotoDbContext db, SeatAllocationService seats)
        {
            _db = db;
            _seats = seats;
        }

        public record ConteoDto(
            string[] categorias,
            int[] votos,
            double[] porcentajes,
            int[]? escanosDhondt,
            int[]? escanosWebster,
            int totalVotos
        );

        [HttpGet("{eleccionId:int}/conteo")]
        public async Task<ActionResult<ConteoDto>> Conteo(int eleccionId)
        {
            var eleccion = await _db.Elecciones.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eleccionId);
            if (eleccion is null) return NotFound();

            if (eleccion.Tipo == TipoEleccion.Nominal)
            {
                var items = await _db.Votos.AsNoTracking()
                    .Where(v => v.EleccionId == eleccionId && v.CandidatoId != null)
                    .GroupBy(v => v.CandidatoId!.Value)
                    .Select(g => new { Id = g.Key, Votos = g.Count() })
                    .OrderBy(x => x.Id)
                    .ToListAsync();

                var nombres = await _db.Candidatos.AsNoTracking()
                    .Where(c => c.EleccionId == eleccionId)
                    .ToDictionaryAsync(c => c.Id, c => c.Nombre);

                var total = items.Sum(x => x.Votos);
                var categorias = items.Select(x => nombres.GetValueOrDefault(x.Id, $"Candidato {x.Id}")).ToArray();
                var votos = items.Select(x => x.Votos).ToArray();
                var porcentajes = votos.Select(v => total == 0 ? 0 : Math.Round(v * 100.0 / total, 2)).ToArray();

                return new ConteoDto(categorias, votos, porcentajes, null, null, total);
            }
            else
            {
                var items = await _db.Votos.AsNoTracking()
                    .Where(v => v.EleccionId == eleccionId && v.ListaId != null)
                    .GroupBy(v => v.ListaId!.Value)
                    .Select(g => new { Id = g.Key, Votos = g.Count() })
                    .OrderBy(x => x.Id)
                    .ToListAsync();

                var nombres = await _db.Listas.AsNoTracking()
                    .Where(l => l.EleccionId == eleccionId)
                    .ToDictionaryAsync(l => l.Id, l => l.Nombre);

                var total = items.Sum(x => x.Votos);
                var categorias = items.Select(x => nombres.GetValueOrDefault(x.Id, $"Lista {x.Id}")).ToArray();
                var votos = items.Select(x => x.Votos).ToArray();
                var porcentajes = votos.Select(v => total == 0 ? 0 : Math.Round(v * 100.0 / total, 2)).ToArray();

                var votosDict = items.ToDictionary(x => x.Id, x => x.Votos);
                var dh = _seats.AsignarEscanos(votosDict, eleccion.NumEscanos, MetodoEscanos.Dhondt);
                var wb = _seats.AsignarEscanos(votosDict, eleccion.NumEscanos, MetodoEscanos.Webster);

                var escDh = items.Select(x => dh[x.Id]).ToArray();
                var escWb = items.Select(x => wb[x.Id]).ToArray();

                return new ConteoDto(categorias, votos, porcentajes, escDh, escWb, total);
            }
        }
    }
}
