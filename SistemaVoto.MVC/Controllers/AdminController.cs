using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaVoto.ApiConsumer;
using SistemaVoto.Modelos;
using SistemaVoto.MVC.ViewModels;

namespace SistemaVoto.MVC.Controllers;

/// <summary>
/// Controlador de administracion - Solo accesible para rol Administrador
/// </summary>
[Authorize(Roles = "Administrador")]
public class AdminController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    #region Dashboard

    /// <summary>
    /// Dashboard principal de administracion
    /// </summary>
    public IActionResult Dashboard()
    {
        var eleccionesResult = Crud<Eleccion>.ReadAll();
        var elecciones = eleccionesResult.Data ?? new List<Eleccion>();
        
        var stats = new DashboardViewModel
        {
            TotalElecciones = elecciones.Count,
            EleccionesActivas = elecciones.Count(e => e.Estado == EstadoEleccion.Activa),
            TotalUsuarios = _userManager.Users.Count(),
            Elecciones = elecciones
        };

        return View(stats);
    }

    #endregion

    #region CRUD Elecciones

    /// <summary>
    /// Lista de elecciones
    /// </summary>
    public IActionResult Elecciones()
    {
        var result = Crud<Eleccion>.ReadAll();
        var elecciones = result.Data ?? new List<Eleccion>();
        return View(elecciones);
    }

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
    public IActionResult CrearEleccion(EleccionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Convertir tipo string a enum
        TipoEleccion tipo = model.Tipo switch
        {
            "Plancha" => TipoEleccion.Plancha,
            "Mixta" => TipoEleccion.Mixta,
            _ => TipoEleccion.Nominal
        };

        // Regla: Si Nominal, NumEscanos=0. Si Plancha o Mixta, NumEscanos>0
        int numEscanos = tipo == TipoEleccion.Nominal ? 0 : Math.Max(1, model.NumEscanos);

        var eleccion = new Eleccion
        {
            Titulo = model.Titulo ?? string.Empty,
            Descripcion = model.Descripcion,
            FechaInicioUtc = model.FechaInicioUtc,
            FechaFinUtc = model.FechaFinUtc,
            Tipo = tipo,
            NumEscanos = numEscanos,
            UsaUbicacion = false,
            ModoUbicacion = ModoUbicacion.Ninguna
        };

        var result = Crud<Eleccion>.Create(eleccion);

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
    public IActionResult EditarEleccion(int id)
    {
        var result = Crud<Eleccion>.ReadById(id);
        
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = "Eleccion no encontrada";
            return RedirectToAction("Elecciones");
        }

        var eleccion = result.Data;
        var model = new EleccionViewModel
        {
            Id = eleccion.Id,
            Titulo = eleccion.Titulo,
            Descripcion = eleccion.Descripcion,
            FechaInicioUtc = eleccion.FechaInicioUtc,
            FechaFinUtc = eleccion.FechaFinUtc,
            Tipo = eleccion.Tipo.ToString(),
            NumEscanos = eleccion.NumEscanos,
            Activo = eleccion.Activo
        };

        return View(model);
    }

    /// <summary>
    /// Procesa la actualizacion de una eleccion
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditarEleccion(EleccionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        TipoEleccion tipo = model.Tipo switch
        {
            "Plancha" => TipoEleccion.Plancha,
            "Mixta" => TipoEleccion.Mixta,
            _ => TipoEleccion.Nominal
        };

        int numEscanos = tipo == TipoEleccion.Nominal ? 0 : Math.Max(1, model.NumEscanos);

        var eleccion = new Eleccion
        {
            Id = model.Id,
            Titulo = model.Titulo ?? string.Empty,
            Descripcion = model.Descripcion,
            FechaInicioUtc = model.FechaInicioUtc,
            FechaFinUtc = model.FechaFinUtc,
            Tipo = tipo,
            NumEscanos = numEscanos,
            Activo = model.Activo
        };

        var result = Crud<Eleccion>.Update(model.Id.ToString(), eleccion);

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
    public IActionResult EliminarEleccion(int id)
    {
        var result = Crud<Eleccion>.Delete(id);
        
        if (result.Success)
            TempData["Success"] = "Eleccion eliminada";
        else
            TempData["Error"] = result.Message ?? "No se pudo eliminar la eleccion";

        return RedirectToAction("Elecciones");
    }

    #endregion

    #region CRUD Candidatos

    /// <summary>
    /// Lista de candidatos (opcionalmente filtrado por eleccion)
    /// </summary>
    public IActionResult Candidatos(int? eleccionId = null)
    {
        var candidatosResult = Crud<Candidato>.ReadAll();
        var eleccionesResult = Crud<Eleccion>.ReadAll();
        
        var candidatos = candidatosResult.Data ?? new List<Candidato>();
        var elecciones = eleccionesResult.Data ?? new List<Eleccion>();

        if (eleccionId.HasValue)
        {
            candidatos = candidatos.Where(c => c.EleccionId == eleccionId.Value).ToList();
        }

        ViewBag.Elecciones = elecciones;
        ViewBag.EleccionIdFilter = eleccionId;
        
        return View(candidatos);
    }

    /// <summary>
    /// Formulario para crear nuevo candidato
    /// </summary>
    [HttpGet]
    public IActionResult CrearCandidato(int? eleccionId = null)
    {
        var eleccionesResult = Crud<Eleccion>.ReadAll();
        ViewBag.Elecciones = eleccionesResult.Data ?? new List<Eleccion>();
        
        return View(new CandidatoViewModel { EleccionId = eleccionId ?? 0 });
    }

    /// <summary>
    /// Procesa la creacion de un candidato
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CrearCandidato(CandidatoViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var eleccionesResult = Crud<Eleccion>.ReadAll();
            ViewBag.Elecciones = eleccionesResult.Data ?? new List<Eleccion>();
            return View(model);
        }

        var candidato = new Candidato
        {
            Nombre = model.Nombre ?? string.Empty,
            PartidoPolitico = model.PartidoPolitico,
            FotoUrl = model.FotoUrl,
            EleccionId = model.EleccionId
        };

        var result = Crud<Candidato>.Create(candidato);

        if (!result.Success)
        {
            model.ErrorMessage = result.Message ?? "Error al crear candidato";
            var eleccionesResult = Crud<Eleccion>.ReadAll();
            ViewBag.Elecciones = eleccionesResult.Data ?? new List<Eleccion>();
            return View(model);
        }

        TempData["Success"] = "Candidato registrado exitosamente";
        return RedirectToAction("Candidatos", new { eleccionId = model.EleccionId });
    }

    /// <summary>
    /// Elimina un candidato
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EliminarCandidato(int id, int? eleccionId = null)
    {
        var result = Crud<Candidato>.Delete(id);
        
        if (result.Success)
            TempData["Success"] = "Candidato eliminado";
        else
            TempData["Error"] = result.Message ?? "No se pudo eliminar el candidato";

        return RedirectToAction("Candidatos", new { eleccionId });
    }

    #endregion

    #region CRUD Usuarios (Identity)

    /// <summary>
    /// Lista de usuarios de Identity
    /// </summary>
    public async Task<IActionResult> UsuariosIdentity()
    {
        var usuarios = _userManager.Users.ToList();
        var viewModels = new List<IdentityUserViewModel>();
        
        foreach (var user in usuarios)
        {
            var roles = await _userManager.GetRolesAsync(user);
            viewModels.Add(new IdentityUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList()
            });
        }
        
        return View(viewModels);
    }

    /// <summary>
    /// Formulario para registrar usuario
    /// </summary>
    [HttpGet]
    public IActionResult RegistrarUsuario()
    {
        return View(new RegistroUsuarioViewModel());
    }

    /// <summary>
    /// Procesa el registro de usuario
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarUsuario(RegistroUsuarioViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Rol);
            TempData["Success"] = $"Usuario {model.Email} creado exitosamente";
            return RedirectToAction("UsuariosIdentity");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    /// <summary>
    /// Elimina un usuario de Identity
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarUsuarioIdentity(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        
        if (user == null)
        {
            TempData["Error"] = "Usuario no encontrado";
            return RedirectToAction("UsuariosIdentity");
        }

        var result = await _userManager.DeleteAsync(user);
        
        if (result.Succeeded)
            TempData["Success"] = "Usuario eliminado";
        else
            TempData["Error"] = "Error al eliminar usuario";

        return RedirectToAction("UsuariosIdentity");
    }

    #endregion

    #region Historial de Votos

    /// <summary>
    /// Historial de votos anonimizados para auditoria
    /// </summary>
    public IActionResult HistorialVotos(int? eleccionId = null)
    {
        var votosResult = Crud<Voto>.ReadAll();
        var eleccionesResult = Crud<Eleccion>.ReadAll();
        
        var votos = votosResult.Data ?? new List<Voto>();
        var elecciones = eleccionesResult.Data ?? new List<Eleccion>();

        if (eleccionId.HasValue)
        {
            votos = votos.Where(v => v.EleccionId == eleccionId.Value).ToList();
        }

        ViewBag.Elecciones = elecciones;
        ViewBag.EleccionIdFilter = eleccionId;
        
        return View(votos);
    }

    #endregion
}

// ViewModels locales para Dashboard
public class DashboardViewModel
{
    public int TotalElecciones { get; set; }
    public int EleccionesActivas { get; set; }
    public int TotalUsuarios { get; set; }
    public int TotalVotos { get; set; }
    public List<SistemaVoto.Modelos.Eleccion> Elecciones { get; set; } = new();
}
