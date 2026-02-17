using System.ComponentModel.DataAnnotations;

namespace SistemaVoto.Api.Dtos
{
    public class VotarRequest
    {
        [Required]
        public string UsuarioId { get; set; } = null!;

        // XOR: uno u otro
        public int? CandidatoId { get; set; }
        public int? ListaId { get; set; }

        // Opcionales: solo se exigen si Eleccion.UsaUbicacion=true
        public int? UbicacionId { get; set; }
        public int? RecintoId { get; set; }
    }
}
