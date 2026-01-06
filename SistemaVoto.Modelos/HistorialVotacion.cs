using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SistemaVoto.Modelos
{
    // PADRÓN: registra que el usuario votó (pero no por quién)
    public class HistorialVotacion
    {
        [Key]
        public int Id { get; set; }

        public int EleccionId { get; set; }

        [ForeignKey(nameof(EleccionId))]
        [JsonIgnore]
        public Eleccion? Eleccion { get; set; }

        public int UsuarioId { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        [JsonIgnore]
        public Usuario? Usuario { get; set; }

        public DateTime FechaParticipacionUtc { get; set; } = DateTime.UtcNow;

        // Hash único de la transacción (auditoría)
        public string? HashTransaccion { get; set; }
    }
}