using Microsoft.AspNetCore.Mvc;
using SistemaVoto.Modelos;
using SistemaVoto.MVC.Services;
using SistemaVoto.MVC.ViewModels;

namespace SistemaVoto.MVC.Controllers;

/// <summary>
/// Controlador para visualizacion de resultados electorales
/// </summary>
public class ResultadosController : Controller
{
    private readonly LocalCrudService _crud;
    private readonly CalculoEscanosService _calculoEscanos;

    public ResultadosController(LocalCrudService crud, CalculoEscanosService calculoEscanos)
    {
        _crud = crud;
        _calculoEscanos = calculoEscanos;
    }

    /// <summary>
    /// Muestra los resultados de una eleccion
    /// </summary>
    /// <summary>
    /// Muestra los resultados de una eleccion
    /// </summary>
    [HttpGet]
    [Route("Resultados/{id}")]
    [Route("Resultados/Index/{id}")]
    public IActionResult Index(int id)
    {
        var eleccion = _crud.GetEleccion(id);
        
        if (eleccion == null)
        {
            TempData["Error"] = "Elección no encontrada";
            return RedirectToAction("Elecciones", "Admin");
        }

        // Obtener candidatos de esta eleccion
        var candidatos = _crud.GetCandidatosByEleccion(id);
        
        // Obtener votos
        var votos = _crud.GetVotosByEleccion(id);
        
        // Obtener listas
        var listas = _crud.GetListasByEleccion(id);

        // Construir resultados
        var totalVotos = votos.Count;
        
        // Resultados Candidatos
        var candidatosConVotos = candidatos.Select(c => new CandidatoResultadoDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            PartidoPolitico = c.PartidoPolitico ?? "",
            Votos = votos.Count(v => v.CandidatoId == c.Id),
            Porcentaje = totalVotos > 0 ? (double)votos.Count(v => v.CandidatoId == c.Id) / totalVotos * 100 : 0
        }).OrderByDescending(c => c.Votos).ToList();

        // Resultados Listas (Para Plancha/Mixta)
        var listasConVotos = listas.Select(l => new ListaResultadoDto
        {
            Id = l.Id,
            Nombre = l.Nombre,
            Votos = votos.Count(v => v.ListaId == l.Id),
            Porcentaje = totalVotos > 0 ? (double)votos.Count(v => v.ListaId == l.Id) / totalVotos * 100 : 0
        }).OrderByDescending(l => l.Votos).ToList();

        // Si es Plancha, y no hay votos directos a candidatos, pero si a listas,
        // atribuimos visualmente los votos de la lista a sus candidatos para que no se vea vacío 
        // OJO: Esto es solo para visualización si se desea, por ahora mantenemos separado.
        // Pero si la elección es Plancha, el grafico principal debería ser de Listas.

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
                Candidatos = candidatosConVotos,
                Listas = listasConVotos
            }
        };

        // Si hay escanos y resultados, calcular distribucion
        if (eleccion.NumEscanos > 0)
        {
            List<(string Nombre, int Votos)> dataParaEscanos = new();

            // Determinar si usamos Votos de Listas o Candidatos
            // Plancha: Se usan listas
            // Nominal: No se calculan escaños proporcionales (regla de negocio: validar si es Nominal)
            // Mixta: Generalmente se asignan escanos por Lista primero (sistema proporcional)
            
            // SOLO calcular si NO es Nominal y tiene escaños > 0
            if (eleccion.Tipo != TipoEleccion.Nominal && (eleccion.Tipo == TipoEleccion.Plancha || eleccion.Tipo == TipoEleccion.Mixta) && listasConVotos.Any())
            {
                dataParaEscanos = listasConVotos.Select(l => (l.Nombre, l.Votos)).ToList();
            }
            else if (candidatosConVotos.Any())
            {
                dataParaEscanos = candidatosConVotos.Select(c => (c.Nombre, c.Votos)).ToList();
            }

            if (dataParaEscanos.Any(x => x.Votos > 0)) // Solo calcular si hay votos
            {
                model.DistribucionDHondt = _calculoEscanos.CalcularDHondt(
                    dataParaEscanos, 
                    eleccion.NumEscanos);
                
                model.DistribucionWebster = _calculoEscanos.CalcularWebster(
                    dataParaEscanos, 
                    eleccion.NumEscanos);
            }
        }

        return View(model);
    }

    /// <summary>
    /// Exporta los resultados a CSV
    /// </summary>
    [HttpGet]
    public IActionResult ExportarCsv(int id)
    {
        var eleccion = _crud.GetEleccion(id);
        if (eleccion == null)
            return NotFound();
        
        // Obtener candidatos y votos
        var candidatos = _crud.GetCandidatosByEleccion(id);
        var votos = _crud.GetVotosByEleccion(id);
        var totalVotos = votos.Count;

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Candidato,Partido,Votos,Porcentaje");
        
        // Resultados Listas (si existen)
        var listas = _crud.GetListasByEleccion(id);
        if (listas.Any())
        {
            csv.AppendLine("--- RESULTADOS POR LISTA ---");
            csv.AppendLine("Lista,Detalle,Votos,Porcentaje");
            foreach (var lista in listas.OrderByDescending(l => votos.Count(v => v.ListaId == l.Id)))
            {
                var votosL = votos.Count(v => v.ListaId == lista.Id);
                var porcentaje = totalVotos > 0 ? (double)votosL / totalVotos * 100 : 0;
                csv.AppendLine($"\"{lista.Nombre}\",\"Lista Completa\",{votosL},{porcentaje:F2}%");
            }
            csv.AppendLine("");
            csv.AppendLine("--- RESULTADOS POR CANDIDATO ---");
        }

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
        var eleccion = _crud.GetEleccion(id);
        if (eleccion == null)
            return Json(new { success = false, message = "Elección no encontrada" });

        // Obtener candidatos y votos
        var candidatos = _crud.GetCandidatosByEleccion(id);
        var votos = _crud.GetVotosByEleccion(id);
        var totalVotos = votos.Count;
        
        var candidatosConVotos = candidatos.Select(c => new
        {
            Nombre = c.Nombre,
            PartidoPolitico = c.PartidoPolitico ?? "",
            Votos = votos.Count(v => v.CandidatoId == c.Id),
            Porcentaje = totalVotos > 0 ? (double)votos.Count(v => v.CandidatoId == c.Id) / totalVotos * 100 : 0
        }).OrderByDescending(c => c.Votos).ToList();

        // Resultados Listas (opcional)
        var listas = _crud.GetListasByEleccion(id);
        var listasConVotos = listas.Select(l => new
        {
            Nombre = l.Nombre,
            Votos = votos.Count(v => v.ListaId == l.Id),
            Porcentaje = totalVotos > 0 ? (double)votos.Count(v => v.ListaId == l.Id) / totalVotos * 100 : 0
        }).OrderByDescending(l => l.Votos).ToList();

        return Json(new { 
            success = true, 
            data = new {
                TotalVotos = totalVotos,
                Candidatos = candidatosConVotos,
                Listas = listasConVotos,
                Tipo = eleccion.Tipo.ToString()
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
