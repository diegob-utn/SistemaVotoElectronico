using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SistemaVoto.Modelos
{
    // Elección puede aplicar a muchas ubicaciones (y viceversa)
    public class EleccionUbicacion
    {
        public int EleccionId { get; set; }
        [JsonIgnore] public Eleccion Eleccion { get; set; } = null!;

        public int UbicacionId { get; set; }
        [JsonIgnore] public Ubicacion Ubicacion { get; set; } = null!;
    }
}