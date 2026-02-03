using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaVoto.Modelos
{
    public class HistorialVoto
    {
        [Key]
        public int Id { get; set; }
        
        public int EleccionId { get; set; }
        
        // ID del usuario de Identity (Guid string)
        public string UsuarioId { get; set; } = string.Empty;
        
        public DateTime FechaVotoUtc { get; set; } = DateTime.UtcNow;
    }
}
