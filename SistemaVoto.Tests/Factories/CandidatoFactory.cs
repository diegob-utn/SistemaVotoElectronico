using SistemaVoto.Modelos;

namespace SistemaVoto.Tests.Factories;

/// <summary>
/// Factory para crear objetos Candidato para pruebas
/// </summary>
public class CandidatoFactory
{
    private int _id = 1;
    private int _eleccionId = 1;
    private string _nombre = "Juan Pérez";
    private string? _partidoPolitico = "Partido Demo";
    private string? _fotoUrl = "https://example.com/foto.jpg";
    private string? _propuestas = "Propuestas de campaña";
    private int? _listaId = null;

    public static CandidatoFactory Nuevo()
    {
        return new CandidatoFactory();
    }

    public static CandidatoFactory ParaEleccion(int eleccionId)
    {
        return new CandidatoFactory { _eleccionId = eleccionId };
    }

    public CandidatoFactory ConId(int id)
    {
        _id = id;
        return this;
    }

    public CandidatoFactory ConNombre(string nombre)
    {
        _nombre = nombre;
        return this;
    }

    public CandidatoFactory ConPartido(string partido)
    {
        _partidoPolitico = partido;
        return this;
    }

    public CandidatoFactory ConPropuestas(string propuestas)
    {
        _propuestas = propuestas;
        return this;
    }

    public CandidatoFactory DeLista(int listaId)
    {
        _listaId = listaId;
        return this;
    }

    public Candidato Build()
    {
        return new Candidato
        {
            Id = _id,
            EleccionId = _eleccionId,
            Nombre = _nombre,
            PartidoPolitico = _partidoPolitico,
            FotoUrl = _fotoUrl,
            Propuestas = _propuestas,
            ListaId = _listaId
        };
    }
}
