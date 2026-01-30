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
    public class Candidato
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Nombre { get; set; } = null!;

        public string? PartidoPolitico { get; set; } // opcional
        public string? FotoUrl { get; set; }
        public string? Propuestas { get; set; }

        public int EleccionId { get; set; }

        [JsonIgnore]
        public Eleccion? Eleccion { get; set; }

        // Plancha: candidato pertenece a una lista
        public int? ListaId { get; set; }

        [JsonIgnore]
        public Lista? Lista { get; set; }
        public string Partido { get; set; }
    }
}