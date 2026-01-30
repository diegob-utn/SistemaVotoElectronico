using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SistemaVoto.Modelos
{
    public class Rol
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Nombre { get; set; } = null!; // "Administrador", "Votante"
        public string Descripcion { get; set; } = null!;
        public bool Activo { get; set; } = true;

        [JsonIgnore]
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}