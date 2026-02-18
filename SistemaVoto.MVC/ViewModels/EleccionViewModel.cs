using SistemaVoto.Modelos;
using System.ComponentModel.DataAnnotations;

namespace SistemaVoto.MVC.ViewModels;

/// <summary>
/// ViewModel para crear/editar elecciones
/// </summary>
public class EleccionViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El titulo es requerido")]
    [StringLength(150, ErrorMessage = "El titulo no puede exceder 150 caracteres")]
    [Display(Name = "Titulo")]
    public string Titulo { get; set; } = null!;

    [Display(Name = "Descripcion")]
    [StringLength(500, ErrorMessage = "La descripcion no puede exceder 500 caracteres")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es requerida")]
    [Display(Name = "Fecha de Inicio")]
    [DataType(DataType.DateTime)]
    public DateTime FechaInicioUtc { get; set; } = DateTime.UtcNow.AddDays(1);

    [Required(ErrorMessage = "La fecha de fin es requerida")]
    [Display(Name = "Fecha de Fin")]
    [DataType(DataType.DateTime)]
    public DateTime FechaFinUtc { get; set; } = DateTime.UtcNow.AddDays(8);

    [Required(ErrorMessage = "El tipo de elección es requerido")]
    [Display(Name = "Tipo de Eleccion")]
    public string Tipo { get; set; } = "Nominal";

    [Display(Name = "Número de Escaños")]
    [Range(0, 1000, ErrorMessage = "El número debe ser mayor o igual a 0")]
    public int NumEscanos { get; set; }

    [Display(Name = "Escaños Nominales (Solo Mixta)")]
    [Range(0, 1000, ErrorMessage = "El número debe ser mayor o igual a 0")]
    public int EscanosNominales { get; set; }

    [Display(Name = "Escaños por Lista (Solo Mixta/Plancha)")]
    [Range(0, 1000, ErrorMessage = "El número debe ser mayor o igual a 0")]
    public int EscanosLista { get; set; }

    // Control de Acceso (Fase 10)
    [Display(Name = "Tipo de Acceso")]
    public string Acceso { get; set; } = "Generada"; // Generada, Privada, Publica

    [Display(Name = "Cupo Máximo de Usuarios (0 = Ilimitado)")]
    [Range(0, 100000, ErrorMessage = "El cupo debe ser mayor o igual a 0")]
    public int CupoMaximo { get; set; } = 0;

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;
    
    [Display(Name = "Usar Restricción Geográfica")]
    public bool UsaUbicacion { get; set; }

    [Display(Name = "Modo de Ubicación")]
    public ModoUbicacion ModoUbicacion { get; set; }

    [Display(Name = "Ubicaciones Habilitadas")]
    public List<int> UbicacionesSeleccionadas { get; set; } = new List<int>();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}






/// <summary>
/// ViewModel para vista de historial de votos
/// </summary>
public class VotoAuditoriaViewModel
{
    public int Id { get; set; }
    public string HashVoto { get; set; } = null!;
    public DateTime FechaVoto { get; set; }
    public string EleccionTitulo { get; set; } = null!;
    public string CandidatoNombre { get; set; } = null!;
    public bool Verificado { get; set; }
}
