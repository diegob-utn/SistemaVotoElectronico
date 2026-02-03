using Microsoft.AspNetCore.Mvc;
using SistemaVoto.ApiConsumer;
using SistemaVoto.Modelos;
using SistemaVoto.MVC.Services;
using SistemaVoto.MVC.ViewModels;

namespace SistemaVoto.MVC.Controllers;

/// <summary>
/// Controlador para visualizacion de resultados electorales
/// </summary>
public class ResultadosController : Controller
{
    private readonly CalculoEscanosService _calculoEscanos;

    public ResultadosController(CalculoEscanosService calculoEscanos)
    {
        _calculoEscanos = calculoEscanos;
    }

    /// <summary>
    /// Muestra los resultados de una eleccion
    /// </summary>
    public IActionResult Index(int id)
    {
        var eleccionResult = Crud<Eleccion>.ReadById(id);
        
        if (!eleccionResult.Success || eleccionResult.Data == null)
        {
            return NotFound();
        }

        var eleccion = eleccionResult.Data;
        
        // Obtener candidatos de esta eleccion
        var candidatosResult = Crud<Candidato>.ReadAll();
        var candidatos = candidatosResult.Data?.Where(c => c.EleccionId == id).ToList() ?? new List<Candidato>();
        
        // Obtener votos
        var votosResult = Crud<Voto>.ReadAll();
        var votos = votosResult.Data?.Where(v => v.EleccionId == id).ToList() ?? new List<Voto>();
        
        // Construir resultados
        var totalVotos = votos.Count;
        var candidatosConVotos = candidatos.Select(c => new CandidatoResultadoDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            PartidoPolitico = c.PartidoPolitico ?? "",
            Votos = votos.Count(v => v.CandidatoId == c.Id),
            Porcentaje = totalVotos > 0 ? (double)votos.Count(v => v.CandidatoId == c.Id) / totalVotos * 100 : 0
        }).OrderByDescending(c => c.Votos).ToList();

        var eleccionDto = new EleccionDto
        {
            Id = eleccion.Id,
            Titulo = eleccion.Titulo,
            Descripcion = eleccion.Descripcion,
            Tipo = eleccion.Tipo.ToString(),
            NumEscanos = eleccion.NumEscanos,
            Estado = eleccion.Estado.ToString(),
            FechaInicioUtc = eleccion.FechaInicioUtc,
            FechaFinUtc = eleccion.FechaFinUtc,
            TotalVotos = totalVotos,
            TotalCandidatos = candidatos.Count
        };

        var model = new ResultadosViewModel
        {
            Eleccion = eleccionDto,
            Resultados = new ResultadosDto
            {
                EleccionId = eleccion.Id,
                Titulo = eleccion.Titulo,
                Tipo = eleccion.Tipo.ToString(),
                NumEscanos = eleccion.NumEscanos,
                TotalVotos = totalVotos,
                Candidatos = candidatosConVotos
            }
        };

        // Si hay escanos y resultados, calcular distribucion
        if (eleccion.NumEscanos > 0 && candidatosConVotos.Any())
        {
            var votosPorCandidato = candidatosConVotos
                .Select(c => (c.Nombre, c.Votos))
                .ToList();
            
            model.DistribucionDHondt = _calculoEscanos.CalcularDHondt(
                votosPorCandidato, 
                eleccion.NumEscanos);
            
            model.DistribucionWebster = _calculoEscanos.CalcularWebster(
                votosPorCandidato, 
                eleccion.NumEscanos);
        }

        return View(model);
    }

    /// <summary>
    /// Exporta los resultados a CSV
    /// </summary>
    [HttpGet]
    public IActionResult ExportarCsv(int id)
    {
        var eleccionResult = Crud<Eleccion>.ReadById(id);
        if (!eleccionResult.Success || eleccionResult.Data == null)
            return NotFound();

        var eleccion = eleccionResult.Data;
        
        // Obtener candidatos y votos
        var candidatosResult = Crud<Candidato>.ReadAll();
        var candidatos = candidatosResult.Data?.Where(c => c.EleccionId == id).ToList() ?? new List<Candidato>();
        
        var votosResult = Crud<Voto>.ReadAll();
        var votos = votosResult.Data?.Where(v => v.EleccionId == id).ToList() ?? new List<Voto>();
        
        var totalVotos = votos.Count;

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Candidato,Partido,Votos,Porcentaje");
        
        foreach (var candidato in candidatos.OrderByDescending(c => votos.Count(v => v.CandidatoId == c.Id)))
        {
            var votosC = votos.Count(v => v.CandidatoId == candidato.Id);
            var porcentaje = totalVotos > 0 ? (double)votosC / totalVotos * 100 : 0;
            csv.AppendLine($"\"{candidato.Nombre}\",\"{candidato.PartidoPolitico}\",{votosC},{porcentaje:F2}%");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        var nombreArchivo = $"resultados_{eleccion.Titulo}_{DateTime.Now:yyyyMMdd}.csv";
        
        return File(bytes, "text/csv", nombreArchivo);
    }

    /// <summary>
    /// Datos JSON para graficos en tiempo real
    /// </summary>
    [HttpGet]
    public IActionResult GetDatosGrafico(int id)
    {
        var eleccionResult = Crud<Eleccion>.ReadById(id);
        if (!eleccionResult.Success || eleccionResult.Data == null)
            return Json(new { success = false, message = "Elección no encontrada" });

        // Obtener candidatos y votos
        var candidatosResult = Crud<Candidato>.ReadAll();
        var candidatos = candidatosResult.Data?.Where(c => c.EleccionId == id).ToList() ?? new List<Candidato>();
        
        var votosResult = Crud<Voto>.ReadAll();
        var votos = votosResult.Data?.Where(v => v.EleccionId == id).ToList() ?? new List<Voto>();
        
        var totalVotos = votos.Count;
        var candidatosConVotos = candidatos.Select(c => new
        {
            Nombre = c.Nombre,
            PartidoPolitico = c.PartidoPolitico ?? "",
            Votos = votos.Count(v => v.CandidatoId == c.Id),
            Porcentaje = totalVotos > 0 ? (double)votos.Count(v => v.CandidatoId == c.Id) / totalVotos * 100 : 0
        }).OrderByDescending(c => c.Votos).ToList();

        return Json(new { 
            success = true, 
            data = new {
                TotalVotos = totalVotos,
                Candidatos = candidatosConVotos
            }
        });
    }
}

// ViewModel para Resultados
public class ResultadosViewModel
{
    public EleccionDto Eleccion { get; set; } = null!;
    public ResultadosDto? Resultados { get; set; }
    public List<(string Nombre, int Escanos)>? DistribucionDHondt { get; set; }
    public List<(string Nombre, int Escanos)>? DistribucionWebster { get; set; }
}
