using Microsoft.AspNetCore.Mvc;
using SistemaVoto.Modelos;
using SistemaVoto.Modelos.Engine;
using SistemaVoto.MVC.Services;
using SistemaVoto.MVC.ViewModels;

namespace SistemaVoto.MVC.Controllers;

/// <summary>
/// Controlador para visualizacion de resultados electorales
/// </summary>
public class ResultadosController : Controller
{
    private readonly LocalCrudService _crud;

    public ResultadosController(LocalCrudService crud)
    {
        _crud = crud;
    }

    /// <summary>
    /// Muestra los resultados de una eleccion
    /// </summary>
    /// <summary>
    /// Muestra los resultados de una eleccion
    /// </summary>
    [HttpGet]
    [Route("Resultados/{id}")]
    [HttpGet]
    [Route("Resultados/Index/{id}")]
    public IActionResult Index(int id)
    {
        var eleccion = _crud.GetEleccion(id);
        
        if (eleccion == null)
        {
            TempData["Error"] = "Elección no encontrada";
            return RedirectToAction("Elecciones", "Admin");
        }

        // Obtener datos crudos
        var candidatos = _crud.GetCandidatosByEleccion(id);
        var votos = _crud.GetVotosByEleccion(id);
        var listas = _crud.GetListasByEleccion(id);
        var totalVotos = votos.Count;

        // --- PREPARAR DATOS PARA EL VIEWMODEL LEGACY (Para mantener views existentes si se requiere) ---
        var candidatosConVotos = candidatos.Select(c => new CandidatoResultadoDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            PartidoPolitico = c.PartidoPolitico ?? "",
            Votos = votos.Count(v => v.CandidatoId == c.Id),
            Porcentaje = totalVotos > 0 ? (double)votos.Count(v => v.CandidatoId == c.Id) / totalVotos * 100 : 0
        }).OrderByDescending(c => c.Votos).ToList();

        var listasConVotos = listas.Select(l => new ListaResultadoDto
        {
            Id = l.Id,
            Nombre = l.Nombre,
            Votos = votos.Count(v => v.ListaId == l.Id),
            Porcentaje = totalVotos > 0 ? (double)votos.Count(v => v.ListaId == l.Id) / totalVotos * 100 : 0
        }).OrderByDescending(l => l.Votos).ToList();

        var model = new ResultadosViewModel
        {
            Eleccion = new EleccionDto 
            {
                Id = eleccion.Id,
                Titulo = eleccion.Titulo,
                Tipo = eleccion.Tipo.ToString(),
                NumEscanos = eleccion.NumEscanos,
                Estado = eleccion.Estado.ToString(),
                TotalVotos = totalVotos
            },
            Resultados = new ResultadosDto
            {
                Candidatos = candidatosConVotos,
                Listas = listasConVotos,
                TotalVotos = totalVotos
            }
        };

        // --- ELECTION ENGINE INTEGRATION ---
        if (eleccion.NumEscanos > 0)
        {
            // 1. Obtener división de escaños desde BD (con fallback para datos legacy)
            int escanosNominales = eleccion.EscanosNominales;
            int escanosLista = eleccion.EscanosLista;

            // Logica de Fallback si no esta definido en BD pero deberia
            if (eleccion.Tipo == TipoEleccion.Mixta && escanosNominales == 0 && escanosLista == 0)
            {
                escanosLista = (int)Math.Ceiling(eleccion.NumEscanos * 0.6); 
                escanosNominales = eleccion.NumEscanos - escanosLista;
            }
            else if (eleccion.Tipo == TipoEleccion.Plancha && escanosLista == 0)
            {
                escanosLista = eleccion.NumEscanos;
            }
            // Para nominal, asumimos todo nominal si no se especifica
            if (eleccion.Tipo == TipoEleccion.Nominal && escanosNominales == 0)
            {
                escanosNominales = eleccion.NumEscanos;
            }

            // 2. Construir Input para Engine
            var engineListas = listas.Select(l => new EngineLista
            {
                Id = l.Id.ToString(),
                Nombre = l.Nombre,
                Votos = votos.Count(v => v.ListaId == l.Id),
                // Asumimos que los candidatos están linkeados a la lista en BD
                Candidatos = _crud.GetCandidatosByEleccion(id)
                    .Where(c => c.ListaId == l.Id) // Asumiendo relación
                    .Select(c => new EngineCandidato { Nombre = c.Nombre, Partido = l.Nombre })
                    .ToList()
            }).ToList();

            // Candidatos nominales (todos los de la elección que reciben votos directos)
            var engineCandidatos = candidatos.Select(c => new EngineCandidato
            {
                Id = c.Id.ToString(),
                Nombre = c.Nombre,
                Partido = c.PartidoPolitico ?? "Independiente",
                Votos = votos.Count(v => v.CandidatoId == c.Id)
            }).ToList();

            var inputDHondt = new EngineInput
            {
                Tipo = MapTipo(eleccion.Tipo),
                Metodo = EngineMetodo.DHondt,
                EscanosTotales = eleccion.NumEscanos,
                EscanosNominales = escanosNominales,
                EscanosLista = escanosLista,
                Listas = engineListas,
                CandidatosNominales = engineCandidatos
            };

            var inputWebster = new EngineInput
            {
                Tipo = MapTipo(eleccion.Tipo),
                Metodo = EngineMetodo.Webster,
                EscanosTotales = eleccion.NumEscanos,
                EscanosNominales = escanosNominales, // Misma división
                EscanosLista = escanosLista,
                Listas = engineListas,
                CandidatosNominales = engineCandidatos
            };

            // 3. Ejecutar Engine si hay datos
            if (votos.Any())
            {
                // D'Hondt
                var resultDHondt = ElectionEngine.Run(inputDHondt);
                
                // Mapear resultado engine a ViewModel
                model.DistribucionDHondt = resultDHondt.DistribucionProporcional
                    .Select(x => (x.Partido, x.Escanos)).ToList();
                
                // Mapear Detalle (convertir tipos del Engine a tipos del ViewModel existentes o nuevos)
                // Usaremos un adaptador simple aquí para reutilizar clases existentes si es posible, o mapear.
                model.DetalleDHondt = MapDetalle(resultDHondt.DetalleProporcional, escanosLista);

                // Webster
                var resultWebster = ElectionEngine.Run(inputWebster);
                model.DistribucionWebster = resultWebster.DistribucionProporcional
                    .Select(x => (x.Partido, x.Escanos)).ToList();
                    
                model.DetalleWebster = MapDetalle(resultWebster.DetalleProporcional, escanosLista);
            }
        }

        return View(model);
    }

    private EngineTipoEleccion MapTipo(TipoEleccion tipo)
    {
        return tipo switch
        {
            TipoEleccion.Nominal => EngineTipoEleccion.Nominal,
            TipoEleccion.Plancha => EngineTipoEleccion.Plancha,
            TipoEleccion.Mixta => EngineTipoEleccion.Mixta,
            _ => EngineTipoEleccion.Plancha
        };
    }

    private DetalleAsignacion? MapDetalle(EngineDetalleCalculo? engineDetalle, int escanos)
    {
        if (engineDetalle == null) return null;

        return new DetalleAsignacion
        {
            Metodo = engineDetalle.Metodo,
            EscanosTotales = escanos,
            Filas = engineDetalle.Filas.Select(f => new FilaAsignacion
            {
               Partido = f.Partido,
               Votos = f.Votos,
               EscanosTotales = f.EscanosGanados,
               ColCocientes = f.Cocientes.Select(c => new Cociente
               {
                   Partido = c.Partido,
                   Valor = c.Valor,
                   Divisor = c.Divisor,
                   EsGanador = c.EsGanador,
                   OrdenAsignacion = c.OrdenAsignacion
               }).ToList()
            }).ToList(),
            CocientesGanadores = engineDetalle.CocientesGanadores.Select(c => new Cociente
            {
                Partido = c.Partido,
                Valor = c.Valor,
                OrdenAsignacion = c.OrdenAsignacion,
                EsGanador = true
            }).ToList()
        };
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
    
    // Detalle detallado
    public DetalleAsignacion? DetalleDHondt { get; set; }
    public DetalleAsignacion? DetalleWebster { get; set; }
}
