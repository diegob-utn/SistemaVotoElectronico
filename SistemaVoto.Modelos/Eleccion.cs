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
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Titulo { get; set; } = null!;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        public DateTime FechaInicioUtc { get; set; }
        public DateTime FechaFinUtc { get; set; }

        public TipoEleccion Tipo { get; set; }     // 0 Nominal, 1 Plancha
        public int NumEscanos { get; set; }        // 0 si Nominal, >0 si Plancha
        public EstadoEleccion Estado { get; set; } = EstadoEleccion.Pendiente;

        public bool UsaUbicacion { get; set; } = false;
        public ModoUbicacion ModoUbicacion { get; set; } = ModoUbicacion.Ninguna;

        public ICollection<Lista> Listas { get; set; } = new List<Lista>();
        public ICollection<Candidato> Candidatos { get; set; } = new List<Candidato>();

        [JsonIgnore]
        public ICollection<Voto> Votos { get; set; } = new List<Voto>();

        [JsonIgnore]
        public ICollection<EleccionUbicacion> EleccionUbicaciones { get; set; } = new List<EleccionUbicacion>();
    }
}