using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaVoto.ApiConsumer;
using SistemaVoto.Modelos;
using SistemaVoto.MVC.ViewModels;
using SistemaVoto.MVC.Services;

namespace SistemaVoto.MVC.Controllers;

/// <summary>
/// Controlador de administracion - Solo accesible para rol Administrador
/// </summary>
[Authorize(Roles = "Administrador")]
public class AdminController : Controller
{
    private readonly LocalCrudService _crud;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ElectionManagerService _electionManager;
    private readonly IWebHostEnvironment _env;

    public AdminController(LocalCrudService crud, UserManager<IdentityUser> userManager, ElectionManagerService electionManager, IWebHostEnvironment env)
    {
        _crud = crud;
        _userManager = userManager;
        _electionManager = electionManager;
        _env = env;
    }

    #region Dashboard

    /// <summary>
    /// Dashboard principal de administracion
    /// </summary>
    public IActionResult Dashboard()
    {
        var elecciones = _crud.GetElecciones();
        
        var votos = _crud.GetVotos();
        
        // Calcular participacion por mes de este año
        var currentYear = DateTime.UtcNow.Year;
        var votosPorMes = votos
            .Where(v => v.FechaVotoUtc.Year == currentYear)
            .GroupBy(v => v.FechaVotoUtc.Month)
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .ToDictionary(k => k.Month, v => v.Count);
            
        var labels = new List<string>();
        var data = new List<int>();
        var meses = new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };
        
        for (int i = 1; i <= DateTime.UtcNow.Month; i++)
        {
            labels.Add(meses[i-1]);
            data.Add(votosPorMes.ContainsKey(i) ? votosPorMes[i] : 0);
        }

        var stats = new DashboardViewModel
        {
            TotalElecciones = elecciones.Count,
            EleccionesActivas = elecciones.Count(e => e.Estado == EstadoEleccion.Activa),
            EleccionesPendientes = elecciones.Count(e => e.Estado == EstadoEleccion.Pendiente),
            EleccionesCerradas = elecciones.Count(e => e.Estado == EstadoEleccion.Cerrada),
            TotalUsuarios = _userManager.Users.Count(),
            TotalVotos = votos.Count,
            Elecciones = elecciones.OrderByDescending(e => e.FechaInicioUtc).Take(5).ToList(),
            ParticipacionData = data,
            ParticipacionLabels = labels
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
        var elecciones = _crud.GetElecciones();
        return View(elecciones);
    }

    /// <summary>
    /// Formulario para crear nueva eleccion
    /// </summary>
    [HttpGet]
    public IActionResult CrearEleccion()
    {
        ViewBag.Ubicaciones = _crud.GetUbicaciones().OrderBy(u => u.Tipo).ThenBy(u => u.Nombre).ToList();
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
        
        TipoAcceso acceso = model.Acceso switch
        {
            "Privada" => TipoAcceso.Privada,
            "Publica" => TipoAcceso.Publica,
            _ => TipoAcceso.Generada
        };

        int numEscanos = Math.Max(1, model.NumEscanos);
        // Solo generar usuarios si es acceso 'Generada'
        // NOTA: En el sistema original, se usaba 'NumEscanos' como cantidad de usuarios a generar si era Nominal.
        // Ahora, si es 'Generada', usaremos 'CupoMaximo' si > 0, o 'NumEscanos' como fallback para mantener compatibilidad,
        // o mejor, asumimos que para 'Generada' el admin debe especificar cuantos.
        // Vamos a usar 'CupoMaximo' como el campo para "Cantidad a Generar" en modo "Generada",
        // y para "Limite de Usuarios" en modo "Privada".
        
        // Si el usuario no puso CupoMaximo en modo Generada, usamos NumEscanos (comportamiento legacy para Nominal)
        int usuariosAGenerar = 0;
        if (acceso == TipoAcceso.Generada)
        {
            usuariosAGenerar = model.CupoMaximo > 0 ? model.CupoMaximo : numEscanos;
        }

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
            ModoUbicacion = model.UsaUbicacion ? model.ModoUbicacion : ModoUbicacion.Ninguna,
            Acceso = acceso,

            CupoMaximo = model.CupoMaximo,
            EscanosNominales = model.EscanosNominales,
            EscanosLista = model.EscanosLista
        };

        // Logic fix: Ensure sums match for Mixed
        if (tipo == TipoEleccion.Mixta && (model.EscanosNominales + model.EscanosLista != numEscanos))
        {
             // Fallback implicit or error? For now update NumEscanos to be safe or just trust inputs
             // Better: If they don't match, prioritize Sum
             if (model.EscanosNominales + model.EscanosLista > 0)
                  eleccion.NumEscanos = model.EscanosNominales + model.EscanosLista;
        }

        try
        {
            // Use Domain Service for complex creation logic (Returns credentials if generated)
            var generatedCredentials = await _electionManager.CreateElectionAsync(eleccion, usuariosAGenerar);

            if (model.UsaUbicacion && model.UbicacionesSeleccionadas != null && model.UbicacionesSeleccionadas.Any())
            {
                foreach (var ubicacionId in model.UbicacionesSeleccionadas)
                {
                    _crud.CreateEleccionUbicacion(new EleccionUbicacion 
                    { 
                        EleccionId = eleccion.Id, 
                        UbicacionId = ubicacionId 
                    });
                }
            }
            
            // Si hay credenciales generadas, mostrar vista
            if (generatedCredentials.Any())
            {
                var credsModel = new CredencialesGeneradasViewModel
                {
                    EleccionId = eleccion.Id,
                    TituloEleccion = eleccion.Titulo,
                    Credenciales = generatedCredentials
                };
                return View("CredencialesGeneradas", credsModel);
            }

            TempData["Success"] = "Elección creada exitosamente";
            return RedirectToAction("Elecciones");
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
            ViewBag.Ubicaciones = _crud.GetUbicaciones().OrderBy(u => u.Tipo).ThenBy(u => u.Nombre).ToList();
            return View(model);
        }
    }



    // GenerateRandomPassword moved to ElectionManagerService to adhere to SOLID / DRY.


    /// <summary>
    /// Formulario para editar eleccion existente
    /// </summary>
    [HttpGet]
    public IActionResult EditarEleccion(int id)
    {
        var eleccion = _crud.GetEleccion(id);
        
        if (eleccion == null)
        {
            TempData["Error"] = "Eleccion no encontrada";
            return RedirectToAction("Elecciones");
        }

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

            ModoUbicacion = eleccion.ModoUbicacion,
            EscanosNominales = eleccion.EscanosNominales,
            EscanosLista = eleccion.EscanosLista
        };
        
        ViewBag.Ubicaciones = _crud.GetUbicaciones().OrderBy(u => u.Tipo).ThenBy(u => u.Nombre).ToList();

        // Cargar selecciones actuales
        var actuales = _crud.GetEleccionUbicacionesByEleccion(id).Select(x => x.UbicacionId).ToList();
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
            ViewBag.Ubicaciones = _crud.GetUbicaciones().OrderBy(u => u.Tipo).ThenBy(u => u.Nombre).ToList();
            return View(model);
        }

        TipoEleccion tipo = model.Tipo switch
        {
            "Plancha" => TipoEleccion.Plancha,
            "Mixta" => TipoEleccion.Mixta,
            _ => TipoEleccion.Nominal
        };

        int numEscanos = Math.Max(1, model.NumEscanos);

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
            ModoUbicacion = model.UsaUbicacion ? model.ModoUbicacion : ModoUbicacion.Ninguna,
            Acceso = model.Acceso switch { "Privada" => TipoAcceso.Privada, "Publica" => TipoAcceso.Publica, _ => TipoAcceso.Generada },
            CupoMaximo = model.CupoMaximo,
            EscanosNominales = model.EscanosNominales,
            EscanosLista = model.EscanosLista
        };

        try
        {
            _crud.UpdateEleccion(eleccion);
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
            ViewBag.Ubicaciones = _crud.GetUbicaciones().OrderBy(u => u.Tipo).ThenBy(u => u.Nombre).ToList();
            return View(model);
        }

        // Actualizar Ubicaciones
        if (model.UsaUbicacion)
        {
             var actuales = _crud.GetEleccionUbicacionesByEleccion(model.Id);
             var actualesIds = actuales.Select(x => x.UbicacionId).ToList();
             var nuevosIds = model.UbicacionesSeleccionadas ?? new List<int>();

             // Borrar removidos
             foreach(var rel in actuales)
             {
                 if (!nuevosIds.Contains(rel.UbicacionId))
                 {
                     _crud.DeleteEleccionUbicacion(rel.Id);
                 }
             }
             
             // Agregar nuevos
             foreach(var nid in nuevosIds)
             {
                 if (!actualesIds.Contains(nid))
                 {
                     _crud.CreateEleccionUbicacion(new EleccionUbicacion { EleccionId = model.Id, UbicacionId = nid });
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
        try
        {
            _crud.DeleteEleccion(id);
            TempData["Success"] = "Eleccion eliminada";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Elecciones");
    }

    /// <summary>
    /// Toggle de activacion/desactivacion de eleccion
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleActivarEleccion(int id)
    {
        var eleccion = _crud.GetEleccion(id);
        
        if (eleccion == null)
        {
            TempData["Error"] = "Elección no encontrada";
            return RedirectToAction("Elecciones");
        }

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

        try
        {
            _crud.UpdateEleccion(eleccion);
            var estadoText = eleccion.Estado == EstadoEleccion.Activa ? "activada" : "desactivada";
            TempData["Success"] = $"Elección {estadoText} exitosamente";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
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
    public IActionResult Usuarios()
    {
        return RedirectToAction("UsuariosIdentity");
    }

    /// <summary>
    /// Lista de usuarios de Identity
    /// </summary>
    public IActionResult UsuariosIdentity()
    {
        var viewModels = _crud.GetUsersWithRoles();
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
        var votos = _crud.GetVotos();
        var elecciones = _crud.GetElecciones();
        
        if (eleccionId.HasValue)
        {
            votos = votos.Where(v => v.EleccionId == eleccionId.Value).ToList();
        }

        // Preparar datos para graficos
        var votosPorHora = votos
            .GroupBy(v => v.FechaVotoUtc.ToLocalTime().Hour)
            .Select(g => new { Hora = g.Key, Cantidad = g.Count() })
            .OrderBy(x => x.Hora)
            .ToList();

        var distribucion = votos
            .GroupBy(v => v.Candidato?.Nombre ?? v.Lista?.Nombre ?? "N/A")
            .Select(g => new { Candidato = g.Key, Cantidad = g.Count() })
            .OrderByDescending(x => x.Cantidad)
            .Take(10) // Top 10
            .ToList();

        ViewBag.DatosPorHora = votosPorHora;
        ViewBag.DatosDistribucion = distribucion;

        ViewBag.Elecciones = elecciones;
        ViewBag.EleccionIdFilter = eleccionId;
        
        return View(votos);
    }

    [HttpGet]
    public IActionResult ExportarHistorialCsv(int? eleccionId)
    {
        var votos = _crud.GetVotos();
        if (eleccionId.HasValue)
            votos = votos.Where(v => v.EleccionId == eleccionId.Value).ToList();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Hash,FechaUtc,Eleccion,Candidato/Lista");
        
        foreach (var v in votos)
        {
            var candidatoOLista = v.Candidato?.Nombre ?? v.Lista?.Nombre ?? "N/A";
            csv.AppendLine($"{v.HashActual},{v.FechaVotoUtc:O},{v.Eleccion?.Titulo},{candidatoOLista}");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"historial_votos_{DateTime.Now:yyyyMMdd}.csv");
    }

    #endregion

    #region Listas y Partidos

    public IActionResult Listas(int? eleccionId)
    {
        var listas = _crud.GetListas();
        var elecciones = _crud.GetElecciones();

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
        ViewBag.Elecciones = _crud.GetElecciones();
        
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
             ViewBag.Elecciones = _crud.GetElecciones();
             return View(model);
        }

        var lista = new Lista
        {
            Nombre = model.Nombre,
            LogoUrl = model.LogoUrl,
            EleccionId = model.EleccionId
        };

        try
        {
            _crud.CreateLista(lista);
            TempData["Success"] = "Lista creada exitosamente";
            return RedirectToAction("Listas", new { eleccionId = model.EleccionId });
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
        }

        ViewBag.Elecciones = _crud.GetElecciones();
        return View(model);
    }

    [HttpGet]
    public IActionResult EditarLista(int id)
    {
        var l = _crud.GetLista(id);
        if (l == null) return RedirectToAction("Listas");

        var eleccion = _crud.GetEleccion(l.EleccionId);
        
        var model = new ListaViewModel
        {
            Id = l.Id,
            Nombre = l.Nombre,
            LogoUrl = l.LogoUrl,
            EleccionId = l.EleccionId,
            EleccionTitulo = eleccion?.Titulo
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
        
        if (_crud.UpdateLista(lista))
        {
            TempData["Success"] = "Lista actualizada";
            return RedirectToAction("Listas", new { eleccionId = model.EleccionId });
        }

        model.ErrorMessage = "No se pudo actualizar la lista.";
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

    public IActionResult Candidatos(int eleccionId = 0, int? listaId = null)
    {
        var candidatos = (eleccionId > 0) 
            ? _crud.GetCandidatosByEleccion(eleccionId) 
            : _crud.GetCandidatos();
            
        if (listaId.HasValue)
        {
            candidatos = candidatos.Where(c => c.ListaId == listaId.Value).ToList();
        }

        var eleccion = eleccionId > 0 ? _crud.GetEleccion(eleccionId) : null;
        var lista = listaId.HasValue ? _crud.GetLista(listaId.Value) : null;

        // Cargar nombres de listas si aplica
        ViewBag.Listas = _crud.GetListas();

        ViewBag.EleccionId = eleccionId;
        ViewBag.Eleccion = eleccion;
        ViewBag.EleccionTitulo = eleccion?.Titulo ?? (lista?.Nombre != null ? $"Lista: {lista.Nombre}" : "Todas las Elecciones");
        
        if (lista != null) ViewBag.ListaFiltro = lista;

        return View(candidatos);
    }

    [HttpGet]
    public IActionResult CrearCandidato(int? eleccionId)
    {
        // Siempre cargar todas las elecciones para el selector
        ViewBag.Elecciones = _crud.GetElecciones();
        
        var model = new CandidatoViewModel();
        
        if (eleccionId.HasValue && eleccionId.Value > 0)
        {
            var eleccion = _crud.GetEleccion(eleccionId.Value);

            model.EleccionId = eleccionId.Value;
            model.EleccionTitulo = eleccion?.Titulo;
            
            // Si es plancha/mixta, cargar listas
            if (eleccion != null)
            {
                ViewBag.Listas = _crud.GetListasByEleccion(eleccionId.Value);
                model.RequiereLista = true; // Permitir seleccionar lista/partido para todos los tipos
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CrearCandidato(CandidatoViewModel model)
    {
        // Validacion personalizada: Requerir Lista segun Tipo de Eleccion
        var eleccion = _crud.GetEleccion(model.EleccionId);
        
        if (eleccion != null && eleccion.Tipo == TipoEleccion.Plancha)
        {
             if (!model.ListaId.HasValue || model.ListaId.Value <= 0)
             {
                 ModelState.AddModelError("ListaId", "En elecciones por Plancha, el candidato debe pertenecer a una Lista.");
             }
        }

        if (model.ListaId.HasValue && model.ListaId.Value > 0)
        {
            // Si selecciono lista, obtener el nombre del partido de la lista
            var lista = _crud.GetLista(model.ListaId.Value);
            if (lista != null)
            {
                model.PartidoPolitico = lista.Nombre; // Asignar nombre de lista como partido
            }
        }
        else if (string.IsNullOrWhiteSpace(model.PartidoPolitico))
        {
             // Para Nominal o Mixta (si es independiente) se requiere partido manual
             if (eleccion?.Tipo != TipoEleccion.Plancha) 
             {
                ModelState.AddModelError("PartidoPolitico", "El partido político o selección de lista es obligatorio.");
             }
        }

        if (!ModelState.IsValid)
        {
            // Recargar elecciones y listas en caso de error
            ViewBag.Elecciones = _crud.GetElecciones();
            ViewBag.Listas = _crud.GetListasByEleccion(model.EleccionId);
            return View(model);
        }

        var candidato = new Candidato
        {
            Nombre = model.Nombre,
            PartidoPolitico = model.PartidoPolitico ?? "Independiente", // Fallback seguro
            FotoUrl = model.FotoUrl,
            Propuestas = model.Propuestas,
            EleccionId = model.EleccionId,
            ListaId = (model.ListaId.HasValue && model.ListaId.Value > 0) ? model.ListaId : null
        };

        try
        {
            _crud.CreateCandidato(candidato);
            TempData["Success"] = "Candidato registrado";
            return RedirectToAction("Candidatos", new { eleccionId = model.EleccionId });
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
        }

        // Recargar listas
        ViewBag.Elecciones = _crud.GetElecciones();
        ViewBag.Listas = _crud.GetListasByEleccion(model.EleccionId);
        return View(model);
    }

    [HttpGet]
    public IActionResult EditarCandidato(int id)
    {
        var c = _crud.GetCandidato(id);
        if (c == null) return RedirectToAction("Elecciones");

        var eleccion = _crud.GetEleccion(c.EleccionId);
        
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

        if (eleccion != null)
        {
             ViewBag.Listas = _crud.GetListasByEleccion(c.EleccionId);
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

        try
        {
            _crud.UpdateCandidato(candidato);
            TempData["Success"] = "Candidato actualizado";
            return RedirectToAction("Candidatos", new { eleccionId = model.EleccionId });
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EliminarCandidato(int id, int eleccionId)
    {
        _crud.DeleteCandidato(id);
        return RedirectToAction("Candidatos", new { eleccionId });
    }

    #endregion
    // ==================== ASIGNACION DE USUARIOS (FASE 10) - DESACTIVADO TEMPORALMENTE ====================
    // Pendiente migración a Identity completo para manejo de elecciones privadas

    [HttpGet]
    public async Task<IActionResult> AsignarUsuarios(int id)
    {
        var eleccion = _crud.GetEleccion(id);
        if (eleccion == null) return RedirectToAction("Elecciones");

        if (eleccion.Acceso != TipoAcceso.Privada)
        {
            TempData["Error"] = "Solo se pueden asignar usuarios a elecciones Privadas.";
            return RedirectToAction("Elecciones");
        }

        // Obtener usuarios del rol votante (Identity)
        // Nota: Asegurarse de que el rol "Usuario" exista y tenga usuarios
        var users = await _userManager.GetUsersInRoleAsync("Usuario");
        // Fallback si no hay rol o usuarios: traer todos
        if (users == null || !users.Any())
        {
             users = _userManager.Users.ToList();
        }

        // Obtener asignaciones actuales desde DB
        var asignados = _crud.GetUsuariosAsignados(id).Select(x => x.UsuarioId).ToHashSet();

        var model = new AsignarUsuariosViewModel
        {
            EleccionId = id,
            EleccionTitulo = eleccion.Titulo,
            CupoMaximo = eleccion.CupoMaximo,
            UsuariosAsignadosCount = asignados.Count,
            Usuarios = users.Select(u => new UsuarioAsignacionDto
            {
                Id = u.Id,
                Email = u.Email,
                Nombre = u.UserName,
                Asignado = asignados.Contains(u.Id)
            }).OrderByDescending(u => u.Asignado).ThenBy(u => u.Email).ToList()
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult ToggleAsignacion(int eleccionId, string usuarioId, bool asignar)
    {
        try
        {
            if (asignar)
            {
                 var success = _crud.AsignarUsuarioAEleccion(eleccionId, usuarioId);
                 if (!success) return Json(new { success = false, message = "Error: Cupo lleno o usuario no encontrado." });
            }
            else
            {
                 _crud.RemoverUsuarioDeEleccion(eleccionId, usuarioId);
            }
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

}

// ViewModels locales para Dashboard
public class DashboardViewModel
{
    public int TotalElecciones { get; set; }
    public int EleccionesActivas { get; set; }
    public int EleccionesPendientes { get; set; }
    public int EleccionesCerradas { get; set; }
    public int TotalUsuarios { get; set; }
    public int TotalVotos { get; set; }
    public List<SistemaVoto.Modelos.Eleccion> Elecciones { get; set; } = new();
    
    // Datos para graficos
    public List<int> ParticipacionData { get; set; } = new();
    public List<string> ParticipacionLabels { get; set; } = new();
}
