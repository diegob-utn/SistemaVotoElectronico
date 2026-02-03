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

        private DateTime _fechaParticipacionUtc = DateTime.UtcNow;
        public DateTime FechaParticipacionUtc 
        { 
            get => _fechaParticipacionUtc; 
            set => _fechaParticipacionUtc = DateTime.SpecifyKind(value, DateTimeKind.Utc); 
        }

        // Hash único de la transacción (auditoría)
        public string? HashTransaccion { get; set; }



        public int? UbicacionId { get; set; }
        [JsonIgnore] public Ubicacion? Ubicacion { get; set; }

        public int? RecintoId { get; set; }
        [JsonIgnore] public RecintoElectoral? Recinto { get; set; }
    }
}