using Microsoft.AspNetCore.Identity;

namespace SistemaVoto.MVC.ViewModels
{
    public class CredencialesGeneradasViewModel
    {
        public int EleccionId { get; set; }
        public string TituloEleccion { get; set; } = string.Empty;
        public List<UsuarioCredencial> Credenciales { get; set; } = new List<UsuarioCredencial>();
    }

    public class UsuarioCredencial
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
