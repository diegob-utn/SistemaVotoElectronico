using System;

namespace SistemaVoto.Api.Dtos
{
    public class HistorialVotoDto
    {
        public int EleccionId { get; set; }
        public string UsuarioId { get; set; } = null!;
        public DateTime FechaParticipacionUtc { get; set; }
        public string? HashTransaccion { get; set; }
    }
}
