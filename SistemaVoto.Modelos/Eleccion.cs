using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SistemaVoto.Modelos
{
    public class Eleccion
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Titulo { get; set; } = null!;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        public DateTime FechaInicioUtc { get; set; }
        public DateTime FechaFinUtc { get; set; }

        public TipoEleccion Tipo { get; set; } = TipoEleccion.Nominal;

        // Solo aplica a Plancha (para D’Hondt/Webster)
        public int NumEscanos { get; set; } = 0;

        public EstadoEleccion Estado { get; set; } = EstadoEleccion.Pendiente;

        public ICollection<Candidato> Candidatos { get; set; } = new List<Candidato>();
        public ICollection<Lista> Listas { get; set; } = new List<Lista>();

        [JsonIgnore]
        public ICollection<Voto> Votos { get; set; } = new List<Voto>();

        [JsonIgnore]
        public ICollection<HistorialVotacion> Historial { get; set; } = new List<HistorialVotacion>();
    }
}