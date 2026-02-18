using System.Collections.Generic;

namespace SistemaVoto.MVC.ViewModels;

public class AsignarUsuariosViewModel
{
    public int EleccionId { get; set; }
    public string EleccionTitulo { get; set; } = string.Empty;
    public int CupoMaximo { get; set; }
    public int UsuariosAsignadosCount { get; set; }
    public List<UsuarioAsignacionDto> Usuarios { get; set; } = new();
}

public class UsuarioAsignacionDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Asignado { get; set; }
}
