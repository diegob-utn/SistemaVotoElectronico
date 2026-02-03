using System.ComponentModel.DataAnnotations;

namespace SistemaVoto.MVC.ViewModels
{
    public class UbicacionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Display(Name = "Nombre de la Ubicación")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo es requerido")]
        [StringLength(50, ErrorMessage = "El tipo no puede exceder 50 caracteres")]
        [Display(Name = "Tipo (Ej: Provincia, Ciudad, Campus)")]
        public string Tipo { get; set; } = "GENERICA";

        [Display(Name = "Ubicación Padre")]
        public int? ParentId { get; set; }
        
        public string? ParentNombre { get; set; }
        
        public string? ErrorMessage { get; set; }
    }

    public class RecintoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
        [Display(Name = "Nombre del Recinto")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "La dirección no puede exceder 250 caracteres")]
        [Display(Name = "Dirección Física")]
        public string? Direccion { get; set; }

        [Display(Name = "Ubicación Geográfica")]
        public int? UbicacionId { get; set; }
        
        public string? UbicacionNombre { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
