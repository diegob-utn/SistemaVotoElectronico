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
        if (!result.Success)
        {
            Console.WriteLine($"[ERROR] AdminController.Elecciones: {result.Message}");
            ViewBag.Error = result.Message;
        }
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
            FechaInicioUtc = DateTime.SpecifyKind(model.FechaInicioUtc, DateTimeKind.Utc),
            FechaInicio = DateTime.SpecifyKind(model.FechaInicioUtc, DateTimeKind.Utc), // Legacy fix
            FechaFinUtc = DateTime.SpecifyKind(model.FechaFinUtc, DateTimeKind.Utc),
            Tipo = tipo,
            NumEscanos = numEscanos,
            Activo = model.Activo,
            Estado = model.Activo ? EstadoEleccion.Activa : EstadoEleccion.Pendiente,
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
        // Generar contraseña que cumpla políticas de Identity
        // Mínimo 8 caracteres, 1 mayúscula, 1 minúscula, 1 dígito, 1 especial
        var random = new Random();
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%&*";
        
        // Garantizar al menos uno de cada tipo
        var password = new char[10];
        password[0] = upper[random.Next(upper.Length)];
        password[1] = lower[random.Next(lower.Length)];
        password[2] = digits[random.Next(digits.Length)];
        password[3] = special[random.Next(special.Length)];
        
        // Llenar el resto con caracteres aleatorios de todos los tipos
        var allChars = upper + lower + digits + special;
        for (int i = 4; i < password.Length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }
        
        // Mezclar para que no sea predecible
        return new string(password.OrderBy(_ => random.Next()).ToArray());
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
            Estado = model.Activo ? EstadoEleccion.Activa : EstadoEleccion.Pendiente,
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

    /// <summary>
    /// Toggle de activacion/desactivacion de eleccion
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleActivarEleccion(int id)
    {
        var result = Crud<Eleccion>.ReadById(id);
        
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = "Elección no encontrada";
            return RedirectToAction("Elecciones");
        }

        var eleccion = result.Data;
        
        // Toggle estado
        if (eleccion.Estado == EstadoEleccion.Activa)
        {
            eleccion.Estado = EstadoEleccion.Pendiente;
            eleccion.Activo = false;
        }
        else
        {
            eleccion.Estado = EstadoEleccion.Activa;
            eleccion.Activo = true;
        }

        var updateResult = Crud<Eleccion>.Update(id.ToString(), eleccion);
        
        if (updateResult.Success)
        {
            var estadoText = eleccion.Estado == EstadoEleccion.Activa ? "activada" : "desactivada";
            TempData["Success"] = $"Elección {estadoText} exitosamente";
        }
        else
        {
            TempData["Error"] = updateResult.Message ?? "Error al cambiar estado";
        }

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

    [HttpGet]
    public IActionResult EditarUbicacion(int id)
    {
        var res = Crud<Ubicacion>.ReadById(id);
        if (!res.Success || res.Data == null)
        {
            TempData["Error"] = "Ubicación no encontrada";
            return RedirectToAction("Ubicaciones");
        }

        var u = res.Data;
        var model = new UbicacionViewModel
        {
            Id = u.Id,
            Nombre = u.Nombre,
            Tipo = u.Tipo,
            ParentId = u.ParentId
        };

        var ubicacionesRes = Crud<Ubicacion>.ReadAll();
        ViewBag.Ubicaciones = ubicacionesRes.Data?.Where(x => x.Id != id).ToList() ?? new List<Ubicacion>();
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditarUbicacion(UbicacionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var ubicacionesRes = Crud<Ubicacion>.ReadAll();
            ViewBag.Ubicaciones = ubicacionesRes.Data?.Where(x => x.Id != model.Id).ToList() ?? new List<Ubicacion>();
            return View(model);
        }

        var ubicacion = new Ubicacion
        {
            Id = model.Id,
            Nombre = model.Nombre,
            Tipo = model.Tipo,
            ParentId = model.ParentId == 0 ? null : model.ParentId
        };

        var result = Crud<Ubicacion>.Update(model.Id.ToString(), ubicacion);

        if (!result.Success)
        {
            model.ErrorMessage = result.Message ?? "Error al actualizar ubicación";
            var ubicacionesRes = Crud<Ubicacion>.ReadAll();
            ViewBag.Ubicaciones = ubicacionesRes.Data?.Where(x => x.Id != model.Id).ToList() ?? new List<Ubicacion>();
            return View(model);
        }

        TempData["Success"] = "Ubicación actualizada exitosamente";
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

    [HttpGet]
    public IActionResult EditarRecinto(int id)
    {
        var res = Crud<RecintoElectoral>.ReadById(id);
        if (!res.Success || res.Data == null)
        {
            TempData["Error"] = "Recinto no encontrado";
            return RedirectToAction("Recintos");
        }

        var r = res.Data;
        var model = new RecintoViewModel
        {
            Id = r.Id,
            Nombre = r.Nombre,
            Direccion = r.Direccion,
            UbicacionId = r.UbicacionId
        };

        var ubicacionesRes = Crud<Ubicacion>.ReadAll();
        ViewBag.Ubicaciones = ubicacionesRes.Data ?? new List<Ubicacion>();
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditarRecinto(RecintoViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var ubicacionesRes = Crud<Ubicacion>.ReadAll();
            ViewBag.Ubicaciones = ubicacionesRes.Data ?? new List<Ubicacion>();
            return View(model);
        }

        var recinto = new RecintoElectoral
        {
            Id = model.Id,
            Nombre = model.Nombre,
            Direccion = model.Direccion,
            UbicacionId = model.UbicacionId == 0 ? null : model.UbicacionId
        };

        var result = Crud<RecintoElectoral>.Update(model.Id.ToString(), recinto);

        if (!result.Success)
        {
            model.ErrorMessage = result.Message ?? "Error al actualizar recinto";
            var ubicacionesRes = Crud<Ubicacion>.ReadAll();
            ViewBag.Ubicaciones = ubicacionesRes.Data ?? new List<Ubicacion>();
            return View(model);
        }

        TempData["Success"] = "Recinto actualizado exitosamente";
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

    #region Listas y Partidos

    public IActionResult Listas(int? eleccionId)
    {
        var listasRes = Crud<Lista>.ReadAll();
        var eleccionesRes = Crud<Eleccion>.ReadAll();
        
        var listas = listasRes.Data ?? new List<Lista>();
        var elecciones = eleccionesRes.Data ?? new List<Eleccion>();

        if (eleccionId.HasValue && eleccionId.Value > 0)
        {
            listas = listas.Where(l => l.EleccionId == eleccionId.Value).ToList();
            var eleccion = elecciones.FirstOrDefault(e => e.Id == eleccionId.Value);
            ViewBag.Eleccion = eleccion;
            ViewBag.EleccionId = eleccionId.Value;
            ViewBag.EleccionTitulo = eleccion?.Titulo;
        }

        ViewBag.Elecciones = elecciones;
        return View(listas);
    }

    [HttpGet]
    public IActionResult CrearLista(int? eleccionId)
    {
        var eleccionesRes = Crud<Eleccion>.ReadAll();
        ViewBag.Elecciones = eleccionesRes.Data ?? new List<Eleccion>();
        
        var model = new ListaViewModel
        {
            EleccionId = eleccionId ?? 0
        };
        
        if (eleccionId.HasValue && eleccionId.Value > 0)
        {
            var elecciones = ViewBag.Elecciones as List<Eleccion> ?? new List<Eleccion>();
            var eleccion = elecciones.FirstOrDefault(e => e.Id == eleccionId.Value);
            model.EleccionTitulo = eleccion?.Titulo;
        }
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CrearLista(ListaViewModel model)
    {
        if (!ModelState.IsValid)
        {
             var eleccionesRes = Crud<Eleccion>.ReadAll();
             ViewBag.Elecciones = eleccionesRes.Data ?? new List<Eleccion>();
             return View(model);
        }

        var lista = new Lista
        {
            Nombre = model.Nombre,
            LogoUrl = model.LogoUrl,
            EleccionId = model.EleccionId
        };

        var result = Crud<Lista>.Create(lista);
        if (result.Success)
        {
            TempData["Success"] = "Lista creada exitosamente";
            return RedirectToAction("Listas", new { eleccionId = model.EleccionId });
        }

        model.ErrorMessage = result.Message;
        var eRes = Crud<Eleccion>.ReadAll();
        ViewBag.Elecciones = eRes.Data ?? new List<Eleccion>();
        return View(model);
    }

    [HttpGet]
    public IActionResult EditarLista(int id)
    {
        var res = Crud<Lista>.ReadById(id);
        if (!res.Success || res.Data == null) return RedirectToAction("Listas");

        var l = res.Data;
        var eleccionRes = Crud<Eleccion>.ReadById(l.EleccionId);
        
        var model = new ListaViewModel
        {
            Id = l.Id,
            Nombre = l.Nombre,
            LogoUrl = l.LogoUrl,
            EleccionId = l.EleccionId,
            EleccionTitulo = eleccionRes.Data?.Titulo
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditarLista(ListaViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var lista = new Lista
        {
            Id = model.Id,
            Nombre = model.Nombre,
            LogoUrl = model.LogoUrl,
            EleccionId = model.EleccionId
        };
        
        var result = Crud<Lista>.Update(model.Id.ToString(), lista);
        if (result.Success)
        {
            TempData["Success"] = "Lista actualizada";
            return RedirectToAction("Listas", new { eleccionId = model.EleccionId });
        }

        model.ErrorMessage = result.Message;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EliminarLista(int id, int? eleccionId)
    {
        var res = Crud<Lista>.Delete(id);
        if (res.Success) TempData["Success"] = "Lista eliminada";
        else TempData["Error"] = res.Message;
        
        return RedirectToAction("Listas", new { eleccionId });
    }

    #endregion

    #region Candidatos

    public IActionResult Candidatos(int eleccionId)
    {
        var candRes = Crud<Candidato>.ReadAll();
        var eleccionRes = Crud<Eleccion>.ReadById(eleccionId);

        var candidatos = candRes.Data ?? new List<Candidato>();
        candidatos = candidatos.Where(c => c.EleccionId == eleccionId).ToList();

        // Cargar nombres de listas si aplica
        var listasRes = Crud<Lista>.ReadAll();
        var listas = listasRes.Data ?? new List<Lista>();
        ViewBag.Listas = listas;

        ViewBag.EleccionId = eleccionId;
        ViewBag.Eleccion = eleccionRes.Data;
        ViewBag.EleccionTitulo = eleccionRes.Data?.Titulo;

        return View(candidatos);
    }

    [HttpGet]
    public IActionResult CrearCandidato(int? eleccionId)
    {
        // Siempre cargar todas las elecciones para el selector
        var eleccionesRes = Crud<Eleccion>.ReadAll();
        ViewBag.Elecciones = eleccionesRes.Data ?? new List<Eleccion>();
        
        var model = new CandidatoViewModel();
        
        if (eleccionId.HasValue && eleccionId.Value > 0)
        {
            var eleccionRes = Crud<Eleccion>.ReadById(eleccionId.Value);
            var eleccion = eleccionRes.Data;

            model.EleccionId = eleccionId.Value;
            model.EleccionTitulo = eleccion?.Titulo;
            
            // Si es plancha/mixta, cargar listas
            if (eleccion != null && (eleccion.Tipo == TipoEleccion.Plancha || eleccion.Tipo == TipoEleccion.Mixta))
            {
                var listasRes = Crud<Lista>.ReadAll();
                var listas = listasRes.Data?.Where(l => l.EleccionId == eleccionId.Value).ToList() ?? new List<Lista>();
                ViewBag.Listas = listas;
                model.RequiereLista = true;
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CrearCandidato(CandidatoViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Recargar elecciones y listas en caso de error
            var eleccionesRes = Crud<Eleccion>.ReadAll();
            ViewBag.Elecciones = eleccionesRes.Data ?? new List<Eleccion>();
            
            var listasRes = Crud<Lista>.ReadAll();
            var listas = listasRes.Data?.Where(l => l.EleccionId == model.EleccionId).ToList() ?? new List<Lista>();
            ViewBag.Listas = listas;
            return View(model);
        }

        var candidato = new Candidato
        {
            Nombre = model.Nombre,
            PartidoPolitico = model.PartidoPolitico,
            FotoUrl = model.FotoUrl,
            Propuestas = model.Propuestas,
            EleccionId = model.EleccionId,
            ListaId = model.RequiereLista ? model.ListaId : null
        };

        var result = Crud<Candidato>.Create(candidato);
        if (result.Success)
        {
            TempData["Success"] = "Candidato registrado";
            return RedirectToAction("Candidatos", new { eleccionId = model.EleccionId });
        }

        model.ErrorMessage = result.Message;
        // Recargar listas
        var lRes = Crud<Lista>.ReadAll();
        ViewBag.Listas = lRes.Data?.Where(l => l.EleccionId == model.EleccionId).ToList() ?? new List<Lista>();
        return View(model);
    }

    [HttpGet]
    public IActionResult EditarCandidato(int id)
    {
        var res = Crud<Candidato>.ReadById(id);
        if (!res.Success || res.Data == null) return RedirectToAction("Elecciones");

        var c = res.Data;
        var eleccionRes = Crud<Eleccion>.ReadById(c.EleccionId);
        var eleccion = eleccionRes.Data;

        var model = new CandidatoViewModel
        {
            Id = c.Id,
            Nombre = c.Nombre,
            PartidoPolitico = c.PartidoPolitico,
            FotoUrl = c.FotoUrl,
            Propuestas = c.Propuestas,
            EleccionId = c.EleccionId,
            ListaId = c.ListaId,
            EleccionTitulo = eleccion?.Titulo
        };

        if (eleccion != null && (eleccion.Tipo == TipoEleccion.Plancha || eleccion.Tipo == TipoEleccion.Mixta))
        {
             var listasRes = Crud<Lista>.ReadAll();
             var listas = listasRes.Data?.Where(l => l.EleccionId == c.EleccionId).ToList() ?? new List<Lista>();
             ViewBag.Listas = listas;
             model.RequiereLista = true;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditarCandidato(CandidatoViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var candidato = new Candidato
        {
            Id = model.Id,
            Nombre = model.Nombre,
            PartidoPolitico = model.PartidoPolitico,
            FotoUrl = model.FotoUrl,
            Propuestas = model.Propuestas,
            EleccionId = model.EleccionId,
            ListaId = model.RequiereLista ? model.ListaId : null
        };

        var result = Crud<Candidato>.Update(model.Id.ToString(), candidato);
        if (result.Success)
        {
             TempData["Success"] = "Candidato actualizado";
             return RedirectToAction("Candidatos", new { eleccionId = model.EleccionId });
        }

        model.ErrorMessage = result.Message;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EliminarCandidato(int id, int eleccionId)
    {
        Crud<Candidato>.Delete(id);
        return RedirectToAction("Candidatos", new { eleccionId });
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
