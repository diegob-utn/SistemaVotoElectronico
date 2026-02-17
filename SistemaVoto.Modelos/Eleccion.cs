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

        private DateTime _fechaInicioUtc;
        public DateTime FechaInicioUtc 
        { 
            get => _fechaInicioUtc; 
            set => _fechaInicioUtc = DateTime.SpecifyKind(value, DateTimeKind.Utc); 
        }

        private DateTime _fechaFinUtc;
        public DateTime FechaFinUtc 
        { 
            get => _fechaFinUtc; 
            set => _fechaFinUtc = DateTime.SpecifyKind(value, DateTimeKind.Utc); 
        }

        // Compatibilidad API Legacy - Linked to FechaInicioUtc to keep DB in sync
        public DateTime FechaInicio 
        { 
            get => FechaInicioUtc; 
            set => FechaInicioUtc = value; 
        }

        public TipoEleccion Tipo { get; set; }     // 0 Nominal, 1 Plancha
        public int NumEscanos { get; set; }        // 0 si Nominal, >0 si Plancha
        public int EscanosNominales { get; set; }  // Para Mixta
        public int EscanosLista { get; set; }      // Para Mixta/Plancha
        public EstadoEleccion Estado { get; set; } = EstadoEleccion.Pendiente;

        public bool UsaUbicacion { get; set; } = false;
        public ModoUbicacion ModoUbicacion { get; set; } = ModoUbicacion.Ninguna;

        // Control de Acceso (Fase 10)
        public TipoAcceso Acceso { get; set; } = TipoAcceso.Generada;
        public int CupoMaximo { get; set; } // 0 = Sin limite
        
        // public ICollection<EleccionUsuario> UsuariosAsignados { get; set; } = new List<EleccionUsuario>();

        public ICollection<Lista> Listas { get; set; } = new List<Lista>();
        public ICollection<Candidato> Candidatos { get; set; } = new List<Candidato>();

        [JsonIgnore]
        public ICollection<Voto> Votos { get; set; } = new List<Voto>();

        [JsonIgnore]
        public ICollection<EleccionUbicacion> EleccionUbicaciones { get; set; } = new List<EleccionUbicacion>();
        public bool Activo { get; set; }
    }
}