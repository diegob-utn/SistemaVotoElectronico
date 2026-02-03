using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SistemaVoto.Modelos
{
    public class EleccionUsuario
    {
        public int EleccionId { get; set; }
        
        [ForeignKey(nameof(EleccionId))]
        public Eleccion Eleccion { get; set; } = null!;

        [Required]
        public string UsuarioId { get; set; } = null!;
        
        [ForeignKey(nameof(UsuarioId))]
        public IdentityUser Usuario { get; set; } = null!;

        public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
    }
}
