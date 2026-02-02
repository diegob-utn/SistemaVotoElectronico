using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaVoto.MVC.Services;
using SistemaVoto.MVC.ViewModels;

namespace SistemaVoto.MVC.Controllers;

/// <summary>
/// Controlador de administracion - Solo accesible para rol Administrador
/// </summary>
[Authorize(Roles = "Administrador")]
public class AdminController : Controller
{
    private readonly ApiService _api;
    private readonly JwtAuthService _authService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(
        ApiService api, 
        JwtAuthService authService,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _api = api;
        _authService = authService;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <summary>
    /// Dashboard principal de administracion
    /// </summary>
    public async Task<IActionResult> Dashboard()
    {
        var stats = new DashboardViewModel();

        // Obtener estadisticas de la API
        var eleccionesResult = await _api.GetAsync<List<EleccionDto>>("api/elecciones");
        var usuariosResult = await _api.GetAsync<List<UsuarioDto>>("api/usuarios");

        if (eleccionesResult.Success && eleccionesResult.Data != null)
        {
            stats.TotalElecciones = eleccionesResult.Data.Count;
            stats.EleccionesActivas = eleccionesResult.Data.Count(e => e.Estado == "Activa");
            stats.Elecciones = eleccionesResult.Data.Take(5).ToList();
        }

        if (usuariosResult.Success && usuariosResult.Data != null)
        {
            stats.TotalUsuarios = usuariosResult.Data.Count;
            stats.Usuarios = usuariosResult.Data;
        }

        return View(stats);
    }

    /// <summary>
    /// Gestion de usuarios
    /// </summary>
    public async Task<IActionResult> Usuarios()
    {
        var result = await _api.GetAsync<List<UsuarioDto>>("api/usuarios");
        var usuarios = result.Success ? result.Data ?? new List<UsuarioDto>() : new List<UsuarioDto>();
        
        return View(usuarios);
    }

    /// <summary>
    /// Gestion de elecciones
    /// </summary>
    public async Task<IActionResult> Elecciones()
    {
        var result = await _api.GetAsync<List<EleccionDto>>("api/elecciones");
        var elecciones = result.Success ? result.Data ?? new List<EleccionDto>() : new List<EleccionDto>();
        
        return View(elecciones);
    }

    /// <summary>
    /// Endpoint para obtener datos de graficos (JSON)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetChartData(int eleccionId)
    {
        var result = await _api.GetAsync<ResultadosDto>($"api/elecciones/{eleccionId}/resultados");
        
        if (!result.Success || result.Data == null)
        {
            return Json(new { success = false, message = result.Message });
        }

        return Json(new { success = true, data = result.Data });
    }

    #region CRUD Elecciones

    /// <summary>
    /// Formulario para crear nueva eleccion
    /// </summary>
    [HttpGet]
    public IActionResult CrearEleccion()
    {
        return View(new EleccionViewModel());
    }

    /// <summary>
    /// Procesa la creacion de una nueva eleccion
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearEleccion(EleccionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Convertir tipo string a enum int (API espera 0=Nominal, 1=Plancha)
        int tipoInt = model.Tipo switch
        {
            "Plancha" => 1,
            _ => 0 // Nominal
        };

        // Regla API: Si Nominal, NumEscanos=0. Si Plancha, NumEscanos>0
        int numEscanos = tipoInt == 0 ? 0 : Math.Max(1, model.NumEscanos);

        var payload = new
        {
            titulo = model.Titulo,
            descripcion = model.Descripcion,
            fechaInicioUtc = model.FechaInicioUtc,
            fechaFinUtc = model.FechaFinUtc,
            tipo = tipoInt,
            numEscanos = numEscanos,
            usaUbicacion = false,
            modoUbicacion = 0
        };

        var result = await _api.PostAsync<object>("api/elecciones", payload);

        if (!result.Success)
        {
            model.ErrorMessage = result.Message ?? "Error al crear eleccion";
            return View(model);
        }

        TempData["Success"] = "Eleccion creada exitosamente";
        return RedirectToAction("Elecciones");
    }

    /// <summary>
    /// Formulario para editar eleccion existente
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> EditarEleccion(int id)
    {
        var result = await _api.GetAsync<EleccionDto>($"api/elecciones/{id}");
        
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = "Eleccion no encontrada";
            return RedirectToAction("Elecciones");
        }

        var model = new EleccionViewModel
        {
            Id = result.Data.Id,
            Titulo = result.Data.Titulo,
            Descripcion = result.Data.Descripcion,
            FechaInicioUtc = result.Data.FechaInicioUtc,
            FechaFinUtc = result.Data.FechaFinUtc,
            Tipo = result.Data.Tipo,
            NumEscanos = result.Data.NumEscanos,
            Activo = result.Data.Activo
        };

        return View(model);
    }

    /// <summary>
    /// Procesa la actualizacion de una eleccion
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarEleccion(EleccionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var payload = new
        {
            titulo = model.Titulo,
            descripcion = model.Descripcion,
            fechaInicioUtc = model.FechaInicioUtc,
            fechaFinUtc = model.FechaFinUtc,
            tipo = model.Tipo,
            numEscanos = model.NumEscanos,
            activo = model.Activo
        };

        var result = await _api.PutAsync<object>($"api/elecciones/{model.Id}", payload);

        if (!result.Success)
        {
            model.ErrorMessage = result.Message ?? "Error al actualizar eleccion";
            return View(model);
        }

        TempData["Success"] = "Eleccion actualizada exitosamente";
        return RedirectToAction("Elecciones");
    }

    /// <summary>
    /// Elimina una eleccion
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarEleccion(int id)
    {
        var result = await _api.DeleteAsync<object>($"api/elecciones/{id}");

        if (result.Success)
        {
            TempData["Success"] = "Eleccion eliminada exitosamente";
        }
        else
        {
            TempData["Error"] = result.Message ?? "Error al eliminar eleccion";
        }

        return RedirectToAction("Elecciones");
    }

    #endregion

    #region CRUD Candidatos

    /// <summary>
    /// Lista de candidatos
    /// </summary>
    public async Task<IActionResult> Candidatos(int? eleccionId = null)
    {
        var url = eleccionId.HasValue 
            ? $"api/candidatos?eleccionId={eleccionId}" 
            : "api/candidatos";
        
        var result = await _api.GetAsync<List<CandidatoDto>>(url);
        var candidatos = result.Success ? result.Data ?? new List<CandidatoDto>() : new List<CandidatoDto>();
        
        ViewBag.EleccionId = eleccionId;
        return View(candidatos);
    }

    /// <summary>
    /// Formulario para crear candidato
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CrearCandidato(int? eleccionId = null)
    {
        var eleccionesResult = await _api.GetAsync<List<EleccionDto>>("api/elecciones");
        ViewBag.Elecciones = eleccionesResult.Data ?? new List<EleccionDto>();
        
        return View(new CandidatoViewModel { EleccionId = eleccionId ?? 0 });
    }

    /// <summary>
    /// Procesa la creacion de un candidato
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCandidato(CandidatoViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var eleccionesResult = await _api.GetAsync<List<EleccionDto>>("api/elecciones");
            ViewBag.Elecciones = eleccionesResult.Data ?? new List<EleccionDto>();
            return View(model);
        }

        var payload = new
        {
            nombre = model.Nombre,
            partidoPolitico = model.PartidoPolitico,
            fotoUrl = model.FotoUrl,
            eleccionId = model.EleccionId
        };

        var result = await _api.PostAsync<object>("api/candidatos", payload);

        if (!result.Success)
        {
            model.ErrorMessage = result.Message ?? "Error al crear candidato";
            var eleccionesResult = await _api.GetAsync<List<EleccionDto>>("api/elecciones");
            ViewBag.Elecciones = eleccionesResult.Data ?? new List<EleccionDto>();
            return View(model);
        }

        TempData["Success"] = "Candidato creado exitosamente";
        return RedirectToAction("Candidatos", new { eleccionId = model.EleccionId });
    }

    /// <summary>
    /// Elimina un candidato
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarCandidato(int id, int? eleccionId = null)
    {
        var result = await _api.DeleteAsync<object>($"api/candidatos/{id}");

        if (result.Success)
        {
            TempData["Success"] = "Candidato eliminado exitosamente";
        }
        else
        {
            TempData["Error"] = result.Message ?? "Error al eliminar candidato";
        }

        return RedirectToAction("Candidatos", new { eleccionId });
    }

    #endregion

    #region Historial de Votos (Auditoria)

    /// <summary>
    /// Vista de historial de votos para auditoria
    /// </summary>
    public async Task<IActionResult> HistorialVotos(int? eleccionId = null)
    {
        var url = eleccionId.HasValue 
            ? $"api/votos?eleccionId={eleccionId}" 
            : "api/votos";
        
        var result = await _api.GetAsync<List<VotoDto>>(url);
        var votos = result.Success ? result.Data ?? new List<VotoDto>() : new List<VotoDto>();
        
        // Obtener elecciones para filtro
        var eleccionesResult = await _api.GetAsync<List<EleccionDto>>("api/elecciones");
        ViewBag.Elecciones = eleccionesResult.Data ?? new List<EleccionDto>();
        ViewBag.EleccionId = eleccionId;
        
        return View(votos);
    }

    #endregion

    /// <summary>
    /// Muestra lista de usuarios de Identity (AspNetUsers)
    /// </summary>
    public async Task<IActionResult> UsuariosIdentity()
    {
        var users = _userManager.Users.ToList();
        var model = new List<IdentityUserViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            model.Add(new IdentityUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                UserName = user.UserName ?? "",
                Roles = roles.ToList(),
                EmailConfirmed = user.EmailConfirmed
            });
        }

        return View(model);
    }

    /// <summary>
    /// Formulario para registrar nuevo usuario
    /// </summary>
    [HttpGet]
    public IActionResult RegistrarUsuario()
    {
        return View(new RegistroUsuarioViewModel());
    }

    /// <summary>
    /// Procesa el registro de nuevo usuario
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarUsuario(RegistroUsuarioViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Verificar si el email ya existe
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            model.ErrorMessage = "Ya existe un usuario con ese email";
            return View(model);
        }

        // Crear usuario
        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            model.ErrorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
            return View(model);
        }

        // Asignar rol
        if (!string.IsNullOrEmpty(model.Rol))
        {
            if (!await _roleManager.RoleExistsAsync(model.Rol))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Rol));
            }
            await _userManager.AddToRoleAsync(user, model.Rol);
        }

        TempData["Success"] = $"Usuario {model.Email} creado exitosamente con rol {model.Rol}";
        return RedirectToAction("UsuariosIdentity");
    }

    /// <summary>
    /// Elimina un usuario de Identity
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarUsuario(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["Error"] = "Usuario no encontrado";
            return RedirectToAction("UsuariosIdentity");
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            TempData["Success"] = "Usuario eliminado exitosamente";
        }
        else
        {
            TempData["Error"] = "Error al eliminar usuario";
        }

        return RedirectToAction("UsuariosIdentity");
    }
}

// ViewModel para usuarios de Identity
public class IdentityUserViewModel
{
    public string Id { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public List<string> Roles { get; set; } = new();
    public bool EmailConfirmed { get; set; }
}

// DTOs para el Admin Dashboard
public class DashboardViewModel
{
    public int TotalElecciones { get; set; }
    public int EleccionesActivas { get; set; }
    public int TotalUsuarios { get; set; }
    public int TotalVotos { get; set; }
    public List<EleccionDto> Elecciones { get; set; } = new();
    public List<UsuarioDto> Usuarios { get; set; } = new();
}

public class EleccionDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = null!;
    public string? Descripcion { get; set; }
    public DateTime FechaInicioUtc { get; set; }
    public DateTime FechaFinUtc { get; set; }
    public string Tipo { get; set; } = null!;
    public string Estado { get; set; } = null!;
    public int NumEscanos { get; set; }
    public bool Activo { get; set; }
}

public class UsuarioDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? NombreUsuario { get; set; }
    public int RolId { get; set; }
    public string? RolNombre { get; set; }
    public bool Activo { get; set; }
}

public class ResultadosDto
{
    public int EleccionId { get; set; }
    public string Titulo { get; set; } = null!;
    public int TotalVotos { get; set; }
    public List<CandidatoResultadoDto> Candidatos { get; set; } = new();
}

public class CandidatoResultadoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string PartidoPolitico { get; set; } = null!;
    public int Votos { get; set; }
    public double Porcentaje { get; set; }
}

public class CandidatoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? PartidoPolitico { get; set; }
    public string? FotoUrl { get; set; }
    public int EleccionId { get; set; }
    public string? EleccionTitulo { get; set; }
    public string? Propuestas { get; set; }
    public int? ListaId { get; set; }
}

public class VotoDto
{
    public int Id { get; set; }
    public string HashVoto { get; set; } = null!;
    public DateTime FechaVoto { get; set; }
    public int EleccionId { get; set; }
    public string? EleccionTitulo { get; set; }
    public int CandidatoId { get; set; }
    public string? CandidatoNombre { get; set; }
}

