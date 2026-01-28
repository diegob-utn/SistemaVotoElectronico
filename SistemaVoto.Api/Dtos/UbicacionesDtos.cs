using System.ComponentModel.DataAnnotations;

namespace SistemaVoto.Api.Dtos
{
    public class CreateUbicacionRequest
    {
        [Required, StringLength(120)]
        public string Nombre { get; set; } = null!;

        [Required, StringLength(50)]
        public string Tipo { get; set; } = "GENERICA";

        public int? ParentId { get; set; }
    }

    public class UpdateUbicacionRequest
    {
        [Required, StringLength(120)]
        public string Nombre { get; set; } = null!;

        [Required, StringLength(50)]
        public string Tipo { get; set; } = "GENERICA";

        public int? ParentId { get; set; }
    }
}
