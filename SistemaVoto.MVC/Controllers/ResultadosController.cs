using Microsoft.AspNetCore.Mvc;
using SistemaVoto.MVC.Services;
using SistemaVoto.MVC.ViewModels;

namespace SistemaVoto.MVC.Controllers;

/// <summary>
/// Controlador para visualizacion de resultados electorales
/// </summary>
public class ResultadosController : Controller
{
    private readonly ApiService _api;
    private readonly CalculoEscanosService _calculoEscanos;

    public ResultadosController(ApiService api, CalculoEscanosService calculoEscanos)
    {
        _api = api;
        _calculoEscanos = calculoEscanos;
    }

    /// <summary>
    /// Muestra los resultados de una eleccion
    /// </summary>
    public async Task<IActionResult> Index(int id)
    {
        var eleccionResult = await _api.GetAsync<EleccionDto>($"api/elecciones/{id}");
        
        if (!eleccionResult.Success || eleccionResult.Data == null)
        {
            return NotFound();
        }

        var resultadosResult = await _api.GetAsync<ResultadosDto>($"api/elecciones/{id}/resultados");
        
        var model = new ResultadosViewModel
        {
            Eleccion = eleccionResult.Data,
            Resultados = resultadosResult.Success ? resultadosResult.Data : null
        };

        // Si hay escanos y resultados, calcular distribucion
        if (model.Eleccion.NumEscanos > 0 && model.Resultados?.Candidatos.Any() == true)
        {
            var votosPorCandidato = model.Resultados.Candidatos
                .Select(c => (c.Nombre, c.Votos))
                .ToList();
            
            model.DistribucionDHondt = _calculoEscanos.CalcularDHondt(
                votosPorCandidato, 
                model.Eleccion.NumEscanos);
            
            model.DistribucionWebster = _calculoEscanos.CalcularWebster(
                votosPorCandidato, 
                model.Eleccion.NumEscanos);
        }

        return View(model);
    }

    /// <summary>
    /// Exporta los resultados a CSV
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportarCsv(int id)
    {
        var eleccionResult = await _api.GetAsync<EleccionDto>($"api/elecciones/{id}");
        var resultadosResult = await _api.GetAsync<ResultadosDto>($"api/elecciones/{id}/resultados");
        
        if (!resultadosResult.Success || resultadosResult.Data == null)
        {
            return NotFound();
        }

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Candidato,Partido,Votos,Porcentaje");
        
        foreach (var candidato in resultadosResult.Data.Candidatos)
        {
            csv.AppendLine($"\"{candidato.Nombre}\",\"{candidato.PartidoPolitico}\",{candidato.Votos},{candidato.Porcentaje:F2}%");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        var nombreArchivo = $"resultados_{eleccionResult.Data?.Titulo ?? "eleccion"}_{DateTime.Now:yyyyMMdd}.csv";
        
        return File(bytes, "text/csv", nombreArchivo);
    }

    /// <summary>
    /// Datos JSON para graficos en tiempo real
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDatosGrafico(int id)
    {
        var resultadosResult = await _api.GetAsync<ResultadosDto>($"api/elecciones/{id}/resultados");
        
        if (!resultadosResult.Success)
        {
            return Json(new { success = false, message = resultadosResult.Message });
        }

        return Json(new { success = true, data = resultadosResult.Data });
    }
}

// ViewModels para Resultados
public class ResultadosViewModel
{
    public EleccionDto Eleccion { get; set; } = null!;
    public ResultadosDto? Resultados { get; set; }
    public List<(string Nombre, int Escanos)>? DistribucionDHondt { get; set; }
    public List<(string Nombre, int Escanos)>? DistribucionWebster { get; set; }
}
