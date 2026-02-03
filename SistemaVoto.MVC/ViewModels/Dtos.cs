namespace SistemaVoto.MVC.ViewModels
{
    /// <summary>
    /// DTO para transferir datos de eleccion
    /// </summary>
    public class EleccionDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public DateTime FechaInicioUtc { get; set; }
        public DateTime FechaFinUtc { get; set; }
        public string Tipo { get; set; } = "Nominal";
        public int NumEscanos { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public bool Activo { get; set; }
        public int TotalVotos { get; set; }
        public int TotalCandidatos { get; set; }
    }

    /// <summary>
    /// DTO para transferir datos de candidato
    /// </summary>
    public class CandidatoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? PartidoPolitico { get; set; }
        public string? FotoUrl { get; set; }
        public string? Propuestas { get; set; }
        public int EleccionId { get; set; }
        public string? EleccionTitulo { get; set; }
        public int? ListaId { get; set; }
        public string? ListaNombre { get; set; }
        public int NumVotos { get; set; }
    }

    /// <summary>
    /// DTO para transferir datos de usuario (API legacy)
    /// </summary>
    public class UsuarioDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string? NombreUsuario { get; set; }
        public string? RolNombre { get; set; }
        public bool Activo { get; set; }
    }

    /// <summary>
    /// DTO para transferir datos de voto (anonimizado para auditoria)
    /// </summary>
    public class VotoDto
    {
        public int Id { get; set; }
        public int EleccionId { get; set; }
        public string? EleccionTitulo { get; set; }
        public int? CandidatoId { get; set; }
        public string? CandidatoNombre { get; set; }
        public DateTime FechaVoto { get; set; }
        public string HashVoto { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para historial de voto
    /// </summary>
    public class HistorialVotoDto
    {
        public int Id { get; set; }
        public int EleccionId { get; set; }
        public int UsuarioId { get; set; }
        public DateTime FechaVotoUtc { get; set; }
    }

    /// <summary>
    /// DTO para resultados de eleccion
    /// </summary>
    public class ResultadosDto
    {
        public int EleccionId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Tipo { get; set; } = "Nominal";
        public int NumEscanos { get; set; }
        public int TotalVotos { get; set; }
        public List<CandidatoResultadoDto> Candidatos { get; set; } = new();
        public List<ListaResultadoDto> Listas { get; set; } = new();
    }

    public class CandidatoResultadoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? PartidoPolitico { get; set; }
        public int Votos { get; set; }
        public double Porcentaje { get; set; }
    }

    public class ListaResultadoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Votos { get; set; }
        public double Porcentaje { get; set; }
    }

    /// <summary>
    /// ViewModel para usuarios de Identity
    /// </summary>
    public class IdentityUserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public bool EmailConfirmed { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
