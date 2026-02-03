using System.ComponentModel.DataAnnotations;

namespace SistemaVoto.MVC.ViewModels
{
    public class ListaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la lista es obligatorio")]
        [StringLength(120)]
        public string Nombre { get; set; } = null!;

        public string? LogoUrl { get; set; }

        public int EleccionId { get; set; }
        public string? EleccionTitulo { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
