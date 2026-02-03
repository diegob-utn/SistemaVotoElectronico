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

    [Required(ErrorMessage = "Seleccione un tipo de eleccion")]
    [Display(Name = "Tipo de Eleccion")]
    public string Tipo { get; set; } = "Nominal";

    [Range(1, 100, ErrorMessage = "El numero de escanos debe estar entre 1 y 100")]
    [Display(Name = "Numero de Escanos")]
    public int NumEscanos { get; set; } = 10;

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
/// ViewModel para crear/editar candidatos
/// </summary>
public class CandidatoViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = null!;

    [StringLength(100, ErrorMessage = "El partido no puede exceder 100 caracteres")]
    [Display(Name = "Partido Politico / Lista")]
    public string? PartidoPolitico { get; set; }

    [Display(Name = "Foto URL")]
    public string? FotoUrl { get; set; }

    [Required(ErrorMessage = "Seleccione una eleccion")]
    [Display(Name = "Eleccion")]
    public int EleccionId { get; set; }

    public string? EleccionTitulo { get; set; }
    
    [Display(Name = "Lista (para Plancha/Mixta)")]
    public int? ListaId { get; set; }
    
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// ViewModel para crear/editar listas
/// </summary>
public class ListaViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    [Display(Name = "Nombre de la Lista/Partido")]
    public string Nombre { get; set; } = null!;

    [Display(Name = "Logo URL")]
    public string? LogoUrl { get; set; }

    [Required]
    public int EleccionId { get; set; }
    
    public string? ErrorMessage { get; set; }
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
