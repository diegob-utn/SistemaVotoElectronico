using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVoto.MVC.Services;

namespace SistemaVoto.MVC.Controllers;

/// <summary>
/// Controlador del sistema de votacion
/// </summary>
[Authorize]
public class VotacionController : Controller
{
    private readonly ApiService _api;
    private readonly JwtAuthService _authService;

    public VotacionController(ApiService api, JwtAuthService authService)
    {
        _api = api;
        _authService = authService;
    }

    /// <summary>
    /// Pantalla de votacion para una eleccion
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(int id)
    {
        // Verificar que la eleccion existe y esta activa
        var eleccionResult = await _api.GetAsync<EleccionDto>($"api/elecciones/{id}");
        
        if (!eleccionResult.Success || eleccionResult.Data == null)
        {
            TempData["Error"] = "Eleccion no encontrada";
            return RedirectToAction("Index", "Elecciones");
        }

        var eleccion = eleccionResult.Data;
        
        if (eleccion.Estado != "Activa")
        {
            TempData["Error"] = "Esta eleccion no esta activa";
            return RedirectToAction("Index", "Elecciones");
        }

        // Verificar si ya voto
        var user = _authService.GetCurrentUser();
        var userId = user?.FindFirst("id")?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            var historialResult = await _api.GetAsync<HistorialVotoDto>($"api/elecciones/{id}/historial/{userId}");
            if (historialResult.Success && historialResult.Data != null)
            {
                TempData["Warning"] = "Ya has votado en esta eleccion";
                return RedirectToAction("YaVoto", new { id });
            }
        }

        // Obtener candidatos
        var candidatosResult = await _api.GetAsync<List<CandidatoDto>>($"api/elecciones/{id}/candidatos");
        
        var model = new VotacionViewModel
        {
            Eleccion = eleccion,
            Candidatos = candidatosResult.Success ? candidatosResult.Data ?? new List<CandidatoDto>() : new List<CandidatoDto>()
        };
        
        return View(model);
    }

    /// <summary>
    /// Procesar el voto
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Votar(int eleccionId, int candidatoId)
    {
        var user = _authService.GetCurrentUser();
        var userId = user?.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            TempData["Error"] = "Sesion expirada";
            return RedirectToAction("Login", "Auth");
        }

        var votoRequest = new
        {
            usuarioId = int.Parse(userId),
            candidatoId = candidatoId
        };

        var result = await _api.PostAsync<object>($"api/elecciones/{eleccionId}/votar", votoRequest);

        if (!result.Success)
        {
            TempData["Error"] = result.Message ?? "Error al registrar el voto";
            return RedirectToAction("Index", new { id = eleccionId });
        }

        TempData["Success"] = "Tu voto ha sido registrado exitosamente";
        return RedirectToAction("Confirmacion", new { id = eleccionId });
    }

    /// <summary>
    /// Pagina de confirmacion de voto
    /// </summary>
    public async Task<IActionResult> Confirmacion(int id)
    {
        var eleccionResult = await _api.GetAsync<EleccionDto>($"api/elecciones/{id}");
        return View(eleccionResult.Data);
    }

    /// <summary>
    /// Pagina cuando el usuario ya voto
    /// </summary>
    public async Task<IActionResult> YaVoto(int id)
    {
        var eleccionResult = await _api.GetAsync<EleccionDto>($"api/elecciones/{id}");
        return View(eleccionResult.Data);
    }
}

// ViewModels para Votacion
public class VotacionViewModel
{
    public EleccionDto Eleccion { get; set; } = null!;
    public List<CandidatoDto> Candidatos { get; set; } = new();
    public int? CandidatoSeleccionado { get; set; }
}
