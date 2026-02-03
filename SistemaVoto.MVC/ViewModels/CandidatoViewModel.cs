using System.ComponentModel.DataAnnotations;

namespace SistemaVoto.MVC.ViewModels
{
    public class CandidatoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del candidato es obligatorio")]
        [StringLength(120)]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El partido político o movimiento es obligatorio")]
        public string PartidoPolitico { get; set; } = null!;

        public string? FotoUrl { get; set; }
        
        [StringLength(500)]
        public string? Propuestas { get; set; }

        public int EleccionId { get; set; }
        public string? EleccionTitulo { get; set; }

        public int? ListaId { get; set; }
        public string? ListaNombre { get; set; }

        // Para filtrar visualmente si aplica lista
        public bool RequiereLista { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
