using System.ComponentModel.DataAnnotations;

namespace SistemaVoto.MVC.ViewModels;

/// <summary>
/// ViewModel para el formulario de login
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = null!;

    [Display(Name = "Recordar sesión")]
    public bool RememberMe { get; set; }

    /// <summary>
    /// URL a la que redirigir después del login exitoso
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Mensaje de error para mostrar en la vista
    /// </summary>
    public string? ErrorMessage { get; set; }
}
