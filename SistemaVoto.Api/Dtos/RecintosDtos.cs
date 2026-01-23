using System.ComponentModel.DataAnnotations;

namespace SistemaVoto.Api.Dtos
{
    public class CreateRecintoRequest
    {
        [Required, StringLength(160)]
        public string Nombre { get; set; } = null!;

        [StringLength(250)]
        public string? Direccion { get; set; }

        public int? UbicacionId { get; set; }
    }

    public class UpdateRecintoRequest : CreateRecintoRequest { }
}
