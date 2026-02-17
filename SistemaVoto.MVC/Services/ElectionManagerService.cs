using Microsoft.AspNetCore.Identity;
using SistemaVoto.Modelos;
using SistemaVoto.MVC.ViewModels;

namespace SistemaVoto.MVC.Services;

/// <summary>
/// Servicio de dominio para gestión de elecciones.
/// Encapsula lógica de negocio compleja como creación con usuarios generados.
/// </summary>
public class ElectionManagerService
{
    private readonly LocalCrudService _crud;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<ElectionManagerService> _logger;

    public ElectionManagerService(LocalCrudService crud, UserManager<IdentityUser> userManager, ILogger<ElectionManagerService> logger)
    {
        _crud = crud;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Crea una elección y genera usuarios si aplica
    /// </summary>
    public async Task<List<UsuarioCredencial>> CreateElectionAsync(Eleccion eleccion, int usuariosAGenerar)
    {
        // 1. Crear la elección en BD
        _crud.CreateEleccion(eleccion);

        var credenciales = new List<UsuarioCredencial>();

        // 2. Generar usuarios si es necesario
        if (eleccion.Acceso == TipoAcceso.Generada && usuariosAGenerar > 0)
        {
            credenciales = await GenerateVotersForElection(eleccion.Id, usuariosAGenerar);
        }

        return credenciales;
    }

    private async Task<List<UsuarioCredencial>> GenerateVotersForElection(int eleccionId, int quantity)
    {
        var credenciales = new List<UsuarioCredencial>();
        
        for (int i = 0; i < quantity; i++)
        {
            // Generar credenciales deterministas: Votante{eleccionId}@{i+1} / Voto#{eleccionId}_{i+1}!
            // Formato de usuario: votante_{eleccionId}_{i+1}@sistema.local
            // Formato de password: Voto#{eleccionId}_{i+1}! (Cumple mayuscula, minuscula, numero y especial)
            
            var index = i + 1;
            var email = $"votante_{eleccionId}_{index}@sistema.local";
            var password = $"Voto#{eleccionId}_{index}!"; 
            
            // Verificar si el usuario ya existe para no fallar (idempotencia basica)
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                // Si existe, reseteamos el password para asegurar que sea el esperado
                var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                var resetResult = await _userManager.ResetPasswordAsync(existingUser, token, password);
                if (resetResult.Succeeded)
                {
                    credenciales.Add(new UsuarioCredencial { Username = email, Password = password });
                }
                else
                {
                    _logger.LogWarning($"Fallo al resetear password usuario existente {email}: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
                }
                continue;
            }

            var user = new IdentityUser 
            { 
                UserName = email, 
                Email = email, 
                EmailConfirmed = true 
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Usuario");
                credenciales.Add(new UsuarioCredencial { Username = email, Password = password });
            }
            else
            {
                _logger.LogWarning($"Fallo al generar usuario {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        return credenciales;
    }
    
    // Removed GenerateRobustPassword as it is no longer used.
}
