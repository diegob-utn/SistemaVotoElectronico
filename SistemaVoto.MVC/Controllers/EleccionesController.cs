using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVoto.MVC.Services;
using SistemaVoto.MVC.ViewModels;

namespace SistemaVoto.MVC.Controllers;

/// <summary>
/// Controlador para gestion y visualizacion de elecciones
/// </summary>
public class EleccionesController : Controller
{
    private readonly ApiService _api;
    private readonly JwtAuthService _authService;

    public EleccionesController(ApiService api, JwtAuthService authService)
    {
        _api = api;
        _authService = authService;
    }

    /// <summary>
    /// Lista de elecciones disponibles
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<List<EleccionDto>>("api/elecciones");
        var elecciones = result.Success ? result.Data ?? new List<EleccionDto>() : new List<EleccionDto>();
        
        // Clasificar por estado
        var model = new EleccionesIndexViewModel
        {
            Activas = elecciones.Where(e => e.Estado == "Activa").ToList(),
            Pendientes = elecciones.Where(e => e.Estado == "Pendiente").ToList(),
            Finalizadas = elecciones.Where(e => e.Estado == "Finalizada").ToList()
        };
        
        return View(model);
    }

    /// <summary>
    /// Detalle de una eleccion especifica
    /// </summary>
    public async Task<IActionResult> Detalle(int id)
    {
        var eleccionResult = await _api.GetAsync<EleccionDto>($"api/elecciones/{id}");
        
        if (!eleccionResult.Success || eleccionResult.Data == null)
        {
            return NotFound();
        }

        var candidatosResult = await _api.GetAsync<List<CandidatoDto>>($"api/elecciones/{id}/candidatos");
        
        var model = new EleccionDetalleViewModel
        {
            Eleccion = eleccionResult.Data,
            Candidatos = candidatosResult.Success ? candidatosResult.Data ?? new List<CandidatoDto>() : new List<CandidatoDto>()
        };
        
        // Verificar si el usuario ya voto
        if (_authService.IsAuthenticated())
        {
            var user = _authService.GetCurrentUser();
            var userId = user?.FindFirst("id")?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                var historialResult = await _api.GetAsync<HistorialVotoDto>($"api/elecciones/{id}/historial/{userId}");
                model.YaVoto = historialResult.Success && historialResult.Data != null;
            }
        }
        
        return View(model);
    }
}

// ViewModels para Elecciones
public class EleccionesIndexViewModel
{
    public List<EleccionDto> Activas { get; set; } = new();
    public List<EleccionDto> Pendientes { get; set; } = new();
    public List<EleccionDto> Finalizadas { get; set; } = new();
}

public class EleccionDetalleViewModel
{
    public EleccionDto Eleccion { get; set; } = null!;
    public List<CandidatoDto> Candidatos { get; set; } = new();
    public bool YaVoto { get; set; }
}

// CandidatoDto esta definido en AdminController

public class HistorialVotoDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int EleccionId { get; set; }
    public DateTime FechaVotoUtc { get; set; }
}

