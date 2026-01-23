using System.ComponentModel.DataAnnotations;
using SistemaVoto.Modelos;

namespace SistemaVoto.Api.Dtos
{
    public class CreateEleccionRequest
    {
        [Required, StringLength(120)]
        public string Titulo { get; set; } = null!;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Required]
        public DateTime FechaInicioUtc { get; set; }

        [Required]
        public DateTime FechaFinUtc { get; set; }

        [Required]
        public TipoEleccion Tipo { get; set; }  // Nominal=0, Plancha=1

        public int NumEscanos { get; set; }     // 0 si Nominal; >0 si Plancha

        public bool UsaUbicacion { get; set; } = false;
        public ModoUbicacion ModoUbicacion { get; set; } = ModoUbicacion.Ninguna;
    }

    public class AssignUbicacionesRequest
    {
        [Required]
        public List<int> UbicacionIds { get; set; } = new();
    }

    public class EleccionListItemDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = null!;
        public string? Descripcion { get; set; }
        public DateTime FechaInicioUtc { get; set; }
        public DateTime FechaFinUtc { get; set; }
        public TipoEleccion Tipo { get; set; }
        public int NumEscanos { get; set; }
        public EstadoEleccion Estado { get; set; }
        public bool UsaUbicacion { get; set; }
        public ModoUbicacion ModoUbicacion { get; set; }
    }
}
