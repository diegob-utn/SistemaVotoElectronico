using System.ComponentModel.DataAnnotations;

namespace SistemaVoto.MVC.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "El nombre completo es requerido")]
    [Display(Name = "Nombre Completo")]
    public string NombreCompleto { get; set; } = null!;

    [Required(ErrorMessage = "El correo electrónico es requerido")]
    [EmailAddress(ErrorMessage = "Formato de correo inválido")]
    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [DataType(DataType.Password)]
    [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} caracteres.", MinimumLength = 6)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = null!;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Contraseña")]
    [Compare("Password", ErrorMessage = "las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = null!;

    public string? ErrorMessage { get; set; }
}
