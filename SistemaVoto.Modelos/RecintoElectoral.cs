using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SistemaVoto.Modelos
{
    public class RecintoElectoral
    {
        public int Id { get; set; }

        [Required, StringLength(160)]
        public string Nombre { get; set; } = null!;

        [StringLength(250)]
        public string? Direccion { get; set; }

        public int? UbicacionId { get; set; }

        [JsonIgnore]
        public Ubicacion? Ubicacion { get; set; }
    }
}