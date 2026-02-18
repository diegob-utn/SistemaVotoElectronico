using Microsoft.AspNetCore.Mvc;
using SistemaVoto.Modelos;
using SistemaVoto.MVC.Services;
using SistemaVoto.MVC.ViewModels;

namespace SistemaVoto.MVC.Controllers;

/// <summary>
/// Controlador para gestion y visualizacion de elecciones públicas (para votantes)
/// </summary>
public class EleccionesController : Controller
{
    private readonly LocalCrudService _crud;

    public EleccionesController(LocalCrudService crud)
    {
        _crud = crud;
    }

    /// <summary>
    /// Lista de elecciones disponibles para el público
    /// </summary>
    public IActionResult Index()
    {
        var todas = _crud.GetElecciones();
        var elecciones = new List<Eleccion>();

        // Lógica de Filtrado de Seguridad
        if (User.IsInRole("Administrador"))
        {
            elecciones = todas;
        }
        else
        {
            // 1. Siempre mostrar públicas
            elecciones.AddRange(todas.Where(e => e.Acceso == TipoAcceso.Publica));

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userName = User.Identity.Name;

                // 2. Mostrar Privadas Asignadas
                if (!string.IsNullOrEmpty(userId))
                {
                    var assignedIds = _crud.GetAssignedElectionIds(userId);
                    elecciones.AddRange(todas.Where(e => assignedIds.Contains(e.Id)));
                }

                // 3. Mostrar Generadas (si corresponde al usuario generado)
                if (userName != null && userName.StartsWith("votante_"))
                {
                    var parts = userName.Split('_');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int userEleccionId))
                    {
                        // Asegurar que se agrega aunque no sea pública ni asignada explícitamente (fallback)
                        var eleccionGenerada = todas.FirstOrDefault(e => e.Id == userEleccionId);
                        if (eleccionGenerada != null)
                        {
                            elecciones.Add(eleccionGenerada);
                        }
                        
                        // Filtrar ESTRICTAMENTE a su elección asignada.
                        elecciones = elecciones.Where(e => 
                            (e.Acceso == TipoAcceso.Generada && e.Titulo.Contains(parts[1])) || // Safety check
                            e.Id == userEleccionId
                        ).ToList();
                    }
                }
            }
            
            // Eliminar duplicados y ordenar
            elecciones = elecciones.DistinctBy(e => e.Id).OrderByDescending(e => e.FechaInicioUtc).ToList();
        }

        // Clasificar por estado
        var model = new EleccionesIndexVM
        {
            Activas = elecciones.Where(e => e.Estado == EstadoEleccion.Activa).Select(MapToDto).ToList(),
            Pendientes = elecciones.Where(e => e.Estado == EstadoEleccion.Pendiente).Select(MapToDto).ToList(),
            Finalizadas = elecciones.Where(e => e.Estado == EstadoEleccion.Cerrada).Select(MapToDto).ToList()
        };
        
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                ViewBag.VotedElectionIds = _crud.GetVotedElectionIds(userId);
            }
        }

        return View(model);
    }

    /// <summary>
    /// Detalle de una eleccion especifica y sus candidatos
    /// </summary>
    public IActionResult Detalle(int id)
    {
        var eleccion = _crud.GetEleccion(id);
        
        if (eleccion == null)
        {
            TempData["Error"] = "Elección no encontrada";
            return RedirectToAction("Index");
        }

        // Verificar si ya ha votado para mostrar mensaje o bloquear boton
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var yaVoto = !string.IsNullOrEmpty(userId) && _crud.HasVoted(id, userId);
        if (yaVoto)
        {
            // Opcional: Redirigir directamente a confirmacion/resultados
            // return RedirectToAction("Confirmacion", new { id = id });
        }

        var candidatos = _crud.GetCandidatosByEleccion(id);
        var listas = _crud.GetListasByEleccion(id);
        
        var model = new EleccionDetalleVM
        {
            Eleccion = MapToDto(eleccion),
            Candidatos = candidatos.Select(c => new CandidatoDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                PartidoPolitico = c.PartidoPolitico ?? "",
                FotoUrl = c.FotoUrl,
                Propuestas = c.Propuestas,
                ListaNombre = listas.FirstOrDefault(l => l.Id == c.ListaId)?.Nombre
            }).ToList(),
            Listas = listas.Select(l => new ListaInfoDto
            {
                Id = l.Id,
                Nombre = l.Nombre,
                LogoUrl = l.LogoUrl,
                CandidatosCount = candidatos.Count(c => c.ListaId == l.Id)
            }).ToList()
        };
        
        // Verificar si el usuario puede votar
        model.PuedeVotar = eleccion.Estado == EstadoEleccion.Activa && 
                          User.Identity?.IsAuthenticated == true;
        
        return View(model);
    }

    /// <summary>
    /// Proceso de votación
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Votar(int id)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            TempData["Error"] = "Debe iniciar sesión para votar";
            return RedirectToAction("Login", "Auth", new { returnUrl = $"/Elecciones/Votar/{id}" });
        }

        var eleccion = _crud.GetEleccion(id);
        if (eleccion == null || eleccion.Estado != EstadoEleccion.Activa)
        {
            TempData["Error"] = "Esta elección no está disponible para votar";
            return RedirectToAction("Index");
        }
        
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        // Verificar Control de Acceso (Fase 10)
        if (!string.IsNullOrEmpty(userId) && !_crud.UsuarioTieneAcceso(id, userId))
        {
             TempData["Error"] = "Usted no está autorizado para votar en esta elección (Requiere asignación).";
             return RedirectToAction("Index");
        }

        // Verificar si ya ha votado
        // var userId ya fue declarado arriba
        if (!string.IsNullOrEmpty(userId) && _crud.HasVoted(id, userId))
        {
             TempData["Error"] = "Usted ya ha votado en esta elección.";
             return RedirectToAction("Confirmacion", new { id = id });
        }

        var candidatos = _crud.GetCandidatosByEleccion(id);
        var listas = _crud.GetListasByEleccion(id);

        var model = new VotarVM
        {
            EleccionId = eleccion.Id,
            EleccionTitulo = eleccion.Titulo,
            TipoEleccion = eleccion.Tipo,
            Candidatos = candidatos.Select(c => new CandidatoDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                PartidoPolitico = c.PartidoPolitico ?? "",
                FotoUrl = c.FotoUrl,
                Propuestas = c.Propuestas,
                ListaNombre = listas.FirstOrDefault(l => l.Id == c.ListaId)?.Nombre
            }).ToList(),
            Listas = listas.Select(l => new ListaInfoDto
            {
                Id = l.Id,
                Nombre = l.Nombre,
                LogoUrl = l.LogoUrl
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Votar(VotarPostVM model)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToAction("Login", "Auth");
        }

        var eleccion = _crud.GetEleccion(model.EleccionId);
        if (eleccion == null || eleccion.Estado != EstadoEleccion.Activa)
        {
            TempData["Error"] = "Esta elección no está disponible para votar";
            return RedirectToAction("Index");
        }

        // Verificar que el candidato o lista pertenezcan a la elección
        if (model.CandidatoId.HasValue)
        {
            var candidato = _crud.GetCandidato(model.CandidatoId.Value);
            if (candidato == null || candidato.EleccionId != model.EleccionId)
            {
                TempData["Error"] = "Candidato inválido";
                return RedirectToAction("Votar", new { id = model.EleccionId });
            }
        }

        // Verificar si ya ha votado (usando Identity User Id)
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        // Verificar Control de Acceso (Fase 10)
        if (!string.IsNullOrEmpty(userId) && !_crud.UsuarioTieneAcceso(model.EleccionId, userId))
        {
             TempData["Error"] = "Usted no está autorizado para votar en esta elección.";
             return RedirectToAction("Index");
        }

        // Validar si el usuario corresponde a esta elección (si es votante generado)
        var userName = User.Identity?.Name;
        if (userName != null && userName.StartsWith("votante_"))
        {
            var parts = userName.Split('_');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int userEleccionId))
            {
                if (userEleccionId != model.EleccionId)
                {
                    TempData["Error"] = "Usted no está autorizado para votar en esta elección.";
                    return RedirectToAction("Index");
                }
            }
        }
        
        // Verificación reforzada de doble voto
        if (!string.IsNullOrEmpty(userId))
        {
            // Check 1: Using ID
            if (_crud.HasVoted(model.EleccionId, userId))
            {
                TempData["Error"] = "Usted ya ha emitido su voto en esta elección.";
                return RedirectToAction("Confirmacion", new { id = model.EleccionId });
            }
        }

        try
        {
            // Crear el voto (anónimo - sin UsuarioId en la tabla Voto)
            var voto = new Voto
            {
                EleccionId = model.EleccionId,
                CandidatoId = model.CandidatoId,
                ListaId = model.ListaId,
                FechaVotoUtc = DateTime.UtcNow,
                HashPrevio = "GENESIS", 
                HashActual = Guid.NewGuid().ToString() // Simple hash generation for now
            };

            _crud.CreateVoto(voto);
            
            // Registrar en historial para prevenir doble voto
            if (!string.IsNullOrEmpty(userId))
            {
                _crud.RegisterVotoHistorial(model.EleccionId, userId);
            }

            TempData["Success"] = "¡Su voto ha sido registrado exitosamente!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al registrar voto: {ex.Message}";
            return RedirectToAction("Votar", new { id = model.EleccionId });
        }

        return RedirectToAction("Confirmacion", new { id = model.EleccionId });
    }

    public IActionResult Confirmacion(int id)
    {
        var eleccion = _crud.GetEleccion(id);
        if (eleccion == null)
        {
            return RedirectToAction("Index");
        }

        ViewBag.EleccionTitulo = eleccion.Titulo;
        return View();
    }

    private static EleccionDto MapToDto(Eleccion e) => new EleccionDto
    {
        Id = e.Id,
        Titulo = e.Titulo,
        Descripcion = e.Descripcion,
        Tipo = e.Tipo.ToString(),
        NumEscanos = e.NumEscanos,
        Estado = e.Estado.ToString(),
        FechaInicioUtc = e.FechaInicioUtc,
        FechaFinUtc = e.FechaFinUtc
    };
}

// ViewModels específicos para Elecciones públicas
public class EleccionesIndexVM
{
    public List<EleccionDto> Activas { get; set; } = new();
    public List<EleccionDto> Pendientes { get; set; } = new();
    public List<EleccionDto> Finalizadas { get; set; } = new();
}

public class EleccionDetalleVM
{
    public EleccionDto Eleccion { get; set; } = null!;
    public List<CandidatoDto> Candidatos { get; set; } = new();
    public List<ListaInfoDto> Listas { get; set; } = new();
    public bool PuedeVotar { get; set; }
}

public class ListaInfoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? LogoUrl { get; set; }
    public int CandidatosCount { get; set; }
}

public class VotarVM
{
    public int EleccionId { get; set; }
    public string EleccionTitulo { get; set; } = "";
    public TipoEleccion TipoEleccion { get; set; }
    public List<CandidatoDto> Candidatos { get; set; } = new();
    public List<ListaInfoDto> Listas { get; set; } = new();
}

public class VotarPostVM
{
    public int EleccionId { get; set; }
    public int? CandidatoId { get; set; }
    public int? ListaId { get; set; }
}
