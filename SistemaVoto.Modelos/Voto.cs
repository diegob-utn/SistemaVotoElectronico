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
    // URNA ANÓNIMA: sin UsuarioId
    // Debe cumplirse: (CandidatoId XOR ListaId)
    public class Voto
    {
        [Key]
        public long Id { get; set; }

        public int EleccionId { get; set; }

        [JsonIgnore]
        public Eleccion? Eleccion { get; set; }

        // Nominal
        public int? CandidatoId { get; set; }
        public Candidato? Candidato { get; set; }

        // Plancha
        public int? ListaId { get; set; }
        public Lista? Lista { get; set; }

        private DateTime _fechaVotoUtc = DateTime.UtcNow;
        public DateTime FechaVotoUtc 
        { 
            get => _fechaVotoUtc; 
            set => _fechaVotoUtc = DateTime.SpecifyKind(value, DateTimeKind.Utc); 
        }

        // Tamper-evident chain
        public string HashPrevio { get; set; } = "GENESIS";
        public string HashActual { get; set; } = null!;

        public int? UbicacionId { get; set; }
        [JsonIgnore] public Ubicacion? Ubicacion { get; set; }

        public int? RecintoId { get; set; }
        [JsonIgnore] public RecintoElectoral? Recinto { get; set; }
    }
}