using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVoto.Api.Services;
using SistemaVoto.Data.Data;
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

        [HttpGet("{eleccionId:int}/export/csv")]
        public async Task<IActionResult> ExportCsv(int eleccionId)
        {
            var actionResult = await Conteo(eleccionId);
            if (actionResult.Result is NotFoundResult) return NotFound();
            
            var dto = actionResult.Value;
            if (dto is null) return NotFound();

            var sb = new StringBuilder();
            sb.AppendLine("Categoria,Votos,Porcentaje");
            
            for (int i = 0; i < dto.categorias.Length; i++)
            {
                sb.AppendLine($"\"{dto.categorias[i]}\",{dto.votos[i]},{dto.porcentajes[i]}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"resultados_{eleccionId}.csv");
        }

        [HttpGet("{eleccionId:int}/export/report")]
        public async Task<IActionResult> ExportReport(int eleccionId)
        {
            var actionResult = await Conteo(eleccionId);
            if (actionResult.Result is NotFoundResult) return NotFound();
            
            var dto = actionResult.Value;
            if (dto is null) return NotFound();

            var sb = new StringBuilder();
            sb.Append("<html><head><title>Reporte Electoral</title>");
            sb.Append("<style>body{font-family:sans-serif; padding:20px;} table{border-collapse:collapse;width:100%;max-width:800px;margin-top:20px;} th,td{border:1px solid #ddd;padding:12px;text-align:left;} th{background-color:#f4f4f4;} h1{color:#333;}</style>");
            sb.Append("</head><body>");
            
            sb.Append($"<h1>Resultados de Elección #{eleccionId}</h1>");
            sb.Append($"<p><strong>Total Votos:</strong> {dto.totalVotos}</p>");
            
            sb.Append("<table><thead><tr><th>Categoría</th><th>Votos</th><th>Porcentaje</th></tr></thead><tbody>");
            
            for (int i = 0; i < dto.categorias.Length; i++)
            {
                sb.Append($"<tr><td>{dto.categorias[i]}</td><td>{dto.votos[i]}</td><td>{dto.porcentajes[i]}%</td></tr>");
            }
            sb.Append("</tbody></table>");
            
            // Si hay escaños
            if (dto.escanosDhondt != null && dto.escanosDhondt.Length > 0)
            {
                sb.Append("<h2>Asignación de Escaños</h2>");
                sb.Append("<table><thead><tr><th>Lista</th><th>D'Hondt</th><th>Webster</th></tr></thead><tbody>");
                for (int i = 0; i < dto.categorias.Length; i++)
                {
                    sb.Append($"<tr><td>{dto.categorias[i]}</td><td>{dto.escanosDhondt[i]}</td><td>{dto.escanosWebster?[i]}</td></tr>");
                }
                 sb.Append("</tbody></table>");
            }

            sb.Append("<div style='margin-top:40px; text-align:center; color:#888; font-size:12px;'>Generado por SistemaVoto</div>");
            sb.Append("<script>window.onload = function() { window.print(); }</script>");
            sb.Append("</body></html>");

            return Content(sb.ToString(), "text/html");
        }
    }
}
