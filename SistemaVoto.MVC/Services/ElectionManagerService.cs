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
            var password = GenerateRobustPassword();
            var email = $"votante_{eleccionId}_{i + 1}@sistema.local";
            
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

    private string GenerateRobustPassword()
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
        
        // Mezclar
        return new string(password.OrderBy(_ => random.Next()).ToArray());
    }
}
