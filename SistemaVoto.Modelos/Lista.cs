using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SistemaVoto.Modelos
{
    public class Lista
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Nombre { get; set; } = null!;

        public string? LogoUrl { get; set; }

        public int EleccionId { get; set; }

        [JsonIgnore]
        public Eleccion? Eleccion { get; set; }

        [JsonIgnore]
        public ICollection<Candidato> Candidatos { get; set; } = new List<Candidato>();
    }
}