using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SistemaVoto.Modelos
{
    // Árbol genérico: País/Provincia/Ciudad/Campus/Facultad/Departamento...
    public class Ubicacion
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Nombre { get; set; } = null!;

        // Flexible: "PROVINCIA", "CIUDAD", "CAMPUS", "FACULTAD", etc.
        [Required, StringLength(50)]
        public string Tipo { get; set; } = "GENERICA";

        public int? ParentId { get; set; }

        [JsonIgnore]
        public Ubicacion? Parent { get; set; }

        [JsonIgnore]
        public ICollection<Ubicacion> Children { get; set; } = new List<Ubicacion>();
    }
}