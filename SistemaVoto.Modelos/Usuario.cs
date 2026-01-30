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
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string NombreCompleto { get; set; } = null!;

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        public int RolId { get; set; }

        [ForeignKey(nameof(RolId))]
        public Rol? Rol { get; set; }

        [JsonIgnore]
        public ICollection<HistorialVotacion> HistorialVotaciones { get; set; } = new List<HistorialVotacion>();
        public string NombreUsuario { get; set; }
        public bool Activo { get; set; }
    }
}