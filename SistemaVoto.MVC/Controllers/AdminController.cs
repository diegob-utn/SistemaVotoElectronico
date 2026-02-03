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
        var ubicacionesRes = Crud<Ubicacion>.ReadAll();
        ViewBag.Ubicaciones = ubicacionesRes.Data?.OrderBy(u => u.Tipo).ThenBy(u => u.Nombre).ToList() ?? new List<Ubicacion>();
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
            var ubicacionesRes = Crud<Ubicacion>.ReadAll();
            ViewBag.Ubicaciones = ubicacionesRes.Data?.OrderBy(u => u.Tipo).ThenBy(u => u.Nombre).ToList() ?? new List<Ubicacion>();
            return View(model);
        }

        TipoEleccion tipo = model.Tipo switch
        {
            "Plancha" => TipoEleccion.Plancha,
            "Mixta" => TipoEleccion.Mixta,
            _ => TipoEleccion.Nominal
        };

        int numEscanos = tipo == TipoEleccion.Nominal ? 0 : Math.Max(1, model.NumEscanos);
        int usuariosAGenerar = model.NumEscanos; // Usamos el input del usuario para decidir cuántos crear

        var eleccion = new Eleccion
        {
            Titulo = model.Titulo ?? string.Empty,
            Descripcion = model.Descripcion,
            FechaInicioUtc = model.FechaInicioUtc,
            FechaInicio = model.FechaInicioUtc, // Legacy fix
            FechaFinUtc = model.FechaFinUtc,
            Tipo = tipo,
            NumEscanos = numEscanos,
            UsaUbicacion = model.UsaUbicacion,
            ModoUbicacion = model.UsaUbicacion ? model.ModoUbicacion : ModoUbicacion.Ninguna
        };

        var result = Crud<Eleccion>.Create(eleccion);

        if (!result.Success)
        {
            model.ErrorMessage = result.Message ?? "Error al crear eleccion";
            var ubicacionesRes = Crud<Ubicacion>.ReadAll();
            ViewBag.Ubicaciones = ubicacionesRes.Data?.OrderBy(u => u.Tipo).ThenBy(u => u.Nombre).ToList() ?? new List<Ubicacion>();
            return View(model);
        }

        if (model.UsaUbicacion && model.UbicacionesSeleccionadas != null && model.UbicacionesSeleccionadas.Any() && result.Data != null)
        {
             foreach(var ubicacionId in model.UbicacionesSeleccionadas)
             {
                 var relacion = new EleccionUbicacion 
                 { 
                     EleccionId = result.Data.Id, 
                     UbicacionId = ubicacionId 
                 };
                 Crud<EleccionUbicacion>.Create(relacion);
             }
        }

        // Generar usuarios automaticos
        var usuariosGenerados = new List<UsuarioCredencial>();
        if (usuariosAGenerar > 0 && result.Data != null)
        {
            for(int i = 0; i < usuariosAGenerar; i++)
            {
                var password = GenerateRandomPassword();
                var username = $"votante_{result.Data.Id}_{i+1}";
                var user = new IdentityUser { UserName = username, Email = $"{username}@sistema.local", EmailConfirmed = true };
                
                var createResult = await _userManager.CreateAsync(user, password);
                if (createResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Usuario");
                    usuariosGenerados.Add(new UsuarioCredencial { Username = username, Password = password });
                }
            }
        }

        if (usuariosGenerados.Any())
        {
            var credsModel = new CredencialesGeneradasViewModel
            {
                EleccionId = result.Data?.Id ?? 0,
                TituloEleccion = result.Data?.Titulo ?? model.Titulo,
                Credenciales = usuariosGenerados
            };
            return View("CredencialesGeneradas", credsModel);
        }

        TempData["Success"] = "Eleccion creada exitosamente";
        return RedirectToAction("Elecciones");
    }

    private string GenerateRandomPassword()
    {
        return "Voto" + Guid.NewGuid().ToString().Substring(0, 6) + "!";
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
            Activo = eleccion.Activo,
            UsaUbicacion = eleccion.UsaUbicacion,
            ModoUbicacion = eleccion.ModoUbicacion
        };
        
        var ubicacionesRes = Crud<Ubicacion>.ReadAll();
        ViewBag.Ubicaciones = ubicacionesRes.Data?.OrderBy(u => u.Tipo).ThenBy(u => u.Nombre).ToList() ?? new List<Ubicacion>();

        // Cargar selecciones actuales
        var asociacionesRes = Crud<EleccionUbicacion>.ReadAll();
        var actuales = asociacionesRes.Data?.Where(x => x.EleccionId == id).Select(x => x.UbicacionId).ToList() ?? new List<int>();
        model.UbicacionesSeleccionadas = actuales;

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
            Activo = model.Activo,
            UsaUbicacion = model.UsaUbicacion,
            ModoUbicacion = model.UsaUbicacion ? model.ModoUbicacion : ModoUbicacion.Ninguna
        };

        var result = Crud<Eleccion>.Update(model.Id.ToString(), eleccion);

        if (!result.Success)
        {
            model.ErrorMessage = result.Message ?? "Error al actualizar eleccion";
            return View(model);
        }

        // Actualizar Ubicaciones
        if (model.UsaUbicacion)
        {
             var asociacionesRes = Crud<EleccionUbicacion>.ReadAll();
             var actuales = asociacionesRes.Data?.Where(x => x.EleccionId == model.Id).ToList() ?? new List<EleccionUbicacion>();
             var actualesIds = actuales.Select(x => x.UbicacionId).ToList();
             var nuevosIds = model.UbicacionesSeleccionadas ?? new List<int>();

             // Borrar removidos
             foreach(var rel in actuales)
             {
                 if (!nuevosIds.Contains(rel.UbicacionId))
                 {
                     Crud<EleccionUbicacion>.Delete(rel.Id);
                 }
             }
             
             // Agregar nuevos
             foreach(var nid in nuevosIds)
             {
                 if (!actualesIds.Contains(nid))
                 {
                     Crud<EleccionUbicacion>.Create(new EleccionUbicacion { EleccionId = model.Id, UbicacionId = nid });
                 }
             }
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
    
    #region Gestion Geografica Eleccion

    [HttpGet]
    public IActionResult GeografiaEleccion(int eleccionId)
    {
        var eleccionRes = Crud<Eleccion>.ReadById(eleccionId);
        if (!eleccionRes.Success || eleccionRes.Data == null) return RedirectToAction("Elecciones");
        
        var eleccion = eleccionRes.Data;
        var ubicacionesRes = Crud<Ubicacion>.ReadAll();
        var asociacionesRes = Crud<EleccionUbicacion>.ReadAll();
        
        var ubicaciones = ubicacionesRes.Data ?? new List<Ubicacion>();
        var asociaciones = asociacionesRes.Data?.Where(x => x.EleccionId == eleccionId).ToList() ?? new List<EleccionUbicacion>();
        
        ViewBag.Eleccion = eleccion;
        ViewBag.AsociacionesIds = asociaciones.Select(a => a.UbicacionId).ToHashSet();
        
        // Enriquecer ubicaciones con nombres de padres para mostrar jerarquía
         foreach (var u in ubicaciones)
        {
            if (u.ParentId.HasValue)
            {
                var parent = ubicaciones.FirstOrDefault(p => p.Id == u.ParentId);
                u.Parent = parent;
            }
        }
        
        return View(ubicaciones.OrderBy(u => u.Tipo).ThenBy(u => u.Nombre).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ActualizarGeografia(int eleccionId, List<int> ubicacionesSeleccionadas)
    {
        var asociacionesRes = Crud<EleccionUbicacion>.ReadAll();
        var actuales = asociacionesRes.Data?.Where(x => x.EleccionId == eleccionId).ToList() ?? new List<EleccionUbicacion>();
        
        // Eliminar las que ya no están seleccionadas
        foreach(var actual in actuales) {
             if(!ubicacionesSeleccionadas.Contains(actual.UbicacionId)) {
                 Crud<EleccionUbicacion>.Delete(actual.Id);
             }
        }
        
        // Agregar las nuevas
        foreach(var id in ubicacionesSeleccionadas) {
             if(!actuales.Any(a => a.UbicacionId == id)) {
                 var nueva = new EleccionUbicacion { EleccionId = eleccionId, UbicacionId = id };
                 Crud<EleccionUbicacion>.Create(nueva);
             }
        }
        
        TempData["Success"] = "Configuración geográfica actualizada";
        return RedirectToAction("Elecciones");
    }
    
    #endregion

    #region CRUD Listas

    /// <summary>
    /// Gestion de Listas para elecciones tipo Plancha/Mixta
    /// </summary>
    public IActionResult Listas(int eleccionId)
    {
        var listasResult = Crud<Lista>.ReadAll();
        var eleccionResult = Crud<Eleccion>.ReadById(eleccionId);
        
        var listas = listasResult.Data?.Where(l => l.EleccionId == eleccionId).ToList() ?? new List<Lista>();
        var eleccion = eleccionResult.Data;

        ViewBag.Eleccion = eleccion;
        ViewBag.EleccionId = eleccionId;
        
        return View(listas);
    }

    [HttpGet]
    public IActionResult CrearLista(int eleccionId)
    {
        return View(new ListaViewModel { EleccionId = eleccionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CrearLista(ListaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var lista = new Lista
        {
            Nombre = model.Nombre,
            LogoUrl = model.LogoUrl,
            EleccionId = model.EleccionId
        };

        var result = Crud<Lista>.Create(lista);

        if (!result.Success)
        {
            model.ErrorMessage = result.Message ?? "Error al crear lista";
            return View(model);
        }

        TempData["Success"] = "Lista creada exitosamente";
        return RedirectToAction("Listas", new { eleccionId = model.EleccionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EliminarLista(int id, int eleccionId)
    {
        var result = Crud<Lista>.Delete(id);
        
        if (result.Success)
            TempData["Success"] = "Lista eliminada";
        else
            TempData["Error"] = result.Message ?? "No se pudo eliminar la lista";

        return RedirectToAction("Listas", new { eleccionId });
    }

    #endregion

    #region CRUD Ubicaciones

    public IActionResult Ubicaciones()
    {
        var result = Crud<Ubicacion>.ReadAll();
        var ubicaciones = result.Data ?? new List<Ubicacion>();
        
        // Cargar nombres de padres para mostrar
        foreach (var u in ubicaciones)
        {
            if (u.ParentId.HasValue)
            {
                var parent = ubicaciones.FirstOrDefault(p => p.Id == u.ParentId);
                u.Parent = parent;
            }
        }

        return View(ubicaciones.OrderBy(u => u.Tipo).ThenBy(u => u.Nombre).ToList());
    }

    [HttpGet]
    public IActionResult CrearUbicacion()
    {
        var result = Crud<Ubicacion>.ReadAll();
        ViewBag.Ubicaciones = result.Data ?? new List<Ubicacion>();
        return View(new UbicacionViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CrearUbicacion(UbicacionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var resultU = Crud<Ubicacion>.ReadAll();
            ViewBag.Ubicaciones = resultU.Data ?? new List<Ubicacion>();
            return View(model);
        }

        var ubicacion = new Ubicacion
        {
            Nombre = model.Nombre,
            Tipo = model.Tipo,
            ParentId = model.ParentId == 0 ? null : model.ParentId
        };

        var result = Crud<Ubicacion>.Create(ubicacion);

        if (!result.Success)
        {
            model.ErrorMessage = result.Message ?? "Error al crear ubicación";
            var resultU = Crud<Ubicacion>.ReadAll();
            ViewBag.Ubicaciones = resultU.Data ?? new List<Ubicacion>();
            return View(model);
        }

        TempData["Success"] = "Ubicación creada exitosamente";
        return RedirectToAction("Ubicaciones");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EliminarUbicacion(int id)
    {
        var result = Crud<Ubicacion>.Delete(id);
        
        if (result.Success)
            TempData["Success"] = "Ubicación eliminada";
        else
            TempData["Error"] = result.Message ?? "No se pudo eliminar (posiblemente tiene hijos o dependencias)";

        return RedirectToAction("Ubicaciones");
    }

    #endregion

    #region CRUD Recintos

    public IActionResult Recintos()
    {
        var recintosResult = Crud<RecintoElectoral>.ReadAll();
        var ubicacionesResult = Crud<Ubicacion>.ReadAll();

        var recintos = recintosResult.Data ?? new List<RecintoElectoral>();
        var ubicaciones = ubicacionesResult.Data ?? new List<Ubicacion>();

        // Enriquecer
        foreach (var r in recintos)
        {
            if (r.UbicacionId.HasValue)
            {
                r.Ubicacion = ubicaciones.FirstOrDefault(u => u.Id == r.UbicacionId);
            }
        }

        return View(recintos);
    }

    [HttpGet]
    public IActionResult CrearRecinto()
    {
        var result = Crud<Ubicacion>.ReadAll();
        ViewBag.Ubicaciones = result.Data ?? new List<Ubicacion>();
        return View(new RecintoViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CrearRecinto(RecintoViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var resultU = Crud<Ubicacion>.ReadAll();
            ViewBag.Ubicaciones = resultU.Data ?? new List<Ubicacion>();
            return View(model);
        }

        var recinto = new RecintoElectoral
        {
            Nombre = model.Nombre,
            Direccion = model.Direccion,
            UbicacionId = model.UbicacionId == 0 ? null : model.UbicacionId
        };

        var result = Crud<RecintoElectoral>.Create(recinto);

        if (!result.Success)
        {
            model.ErrorMessage = result.Message ?? "Error al crear recinto";
            var resultU = Crud<Ubicacion>.ReadAll();
            ViewBag.Ubicaciones = resultU.Data ?? new List<Ubicacion>();
            return View(model);
        }

        TempData["Success"] = "Recinto creado exitosamente";
        return RedirectToAction("Recintos");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EliminarRecinto(int id)
    {
        var result = Crud<RecintoElectoral>.Delete(id);
        
        if (result.Success)
            TempData["Success"] = "Recinto eliminado";
        else
            TempData["Error"] = result.Message ?? "Error al eliminar recinto";

        return RedirectToAction("Recintos");
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
        var listasResult = Crud<Lista>.ReadAll();
        
        var candidatos = candidatosResult.Data ?? new List<Candidato>();
        var elecciones = eleccionesResult.Data ?? new List<Eleccion>();
        var listas = listasResult.Data ?? new List<Lista>();

        if (eleccionId.HasValue)
        {
            candidatos = candidatos.Where(c => c.EleccionId == eleccionId.Value).ToList();
        }

        // Enriquecer candidatos con nombres de listas
        foreach (var cand in candidatos)
        {
            if (cand.ListaId.HasValue)
            {
                cand.Lista = listas.FirstOrDefault(l => l.Id == cand.ListaId.Value);
            }
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
        var listasResult = Crud<Lista>.ReadAll();
        
        ViewBag.Elecciones = eleccionesResult.Data ?? new List<Eleccion>();
        ViewBag.Listas = listasResult.Data ?? new List<Lista>();
        
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
            var listasResult = Crud<Lista>.ReadAll();
            ViewBag.Elecciones = eleccionesResult.Data ?? new List<Eleccion>();
            ViewBag.Listas = listasResult.Data ?? new List<Lista>();
            return View(model);
        }

        var candidato = new Candidato
        {
            Nombre = model.Nombre ?? string.Empty,
            PartidoPolitico = model.PartidoPolitico ?? string.Empty,
            FotoUrl = model.FotoUrl,
            EleccionId = model.EleccionId,
            ListaId = model.ListaId == 0 ? null : model.ListaId // Manejar 0 como null
        };

        var result = Crud<Candidato>.Create(candidato);

        if (!result.Success)
        {
            model.ErrorMessage = result.Message ?? "Error al crear candidato";
            var eleccionesResult = Crud<Eleccion>.ReadAll();
            var listasResult = Crud<Lista>.ReadAll();
            ViewBag.Elecciones = eleccionesResult.Data ?? new List<Eleccion>();
            ViewBag.Listas = listasResult.Data ?? new List<Lista>();
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
