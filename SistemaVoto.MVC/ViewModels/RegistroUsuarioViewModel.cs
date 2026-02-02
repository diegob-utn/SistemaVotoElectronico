using System.ComponentModel.DataAnnotations;

namespace SistemaVoto.MVC.ViewModels;

/// <summary>
/// ViewModel para registrar un nuevo usuario (usado por Admin)
/// </summary>
public class RegistroUsuarioViewModel
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email invalido")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "El nombre es requerido")]
    [Display(Name = "Nombre Completo")]
    public string NombreCompleto { get; set; } = null!;

    [Required(ErrorMessage = "La contrasena es requerida")]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "La contrasena debe tener al menos 4 caracteres")]
    [DataType(DataType.Password)]
    [Display(Name = "Contrasena")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Confirme la contrasena")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Las contrasenas no coinciden")]
    [Display(Name = "Confirmar Contrasena")]
    public string ConfirmPassword { get; set; } = null!;

    [Required(ErrorMessage = "Seleccione un rol")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = "Usuario";

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}
