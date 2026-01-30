using SistemaVoto.Modelos;

namespace SistemaVoto.Tests.Factories;

/// <summary>
/// Factory para crear objetos Voto para pruebas
/// </summary>
public class VotoFactory
{
    private int _id = 1;
    private int _eleccionId = 1;
    private int? _candidatoId = null;
    private int? _listaId = null;
    private DateTime _fechaVoto = DateTime.UtcNow;
    private string _hashPrevio = "GENESIS";
    private string _hashActual = "abc123def456";
    private int? _ubicacionId = null;
    private int? _recintoId = null;

    public static VotoFactory Nuevo()
    {
        return new VotoFactory();
    }

    public static VotoFactory ParaEleccion(int eleccionId)
    {
        return new VotoFactory { _eleccionId = eleccionId };
    }

    // Cambian a métodos de instancia para permitir encadenamiento
    public VotoFactory ParaCandidato(int candidatoId)
    {
        _candidatoId = candidatoId;
        return this;
    }

    public VotoFactory ParaLista(int listaId)
    {
        _listaId = listaId;
        return this;
    }

    public VotoFactory ConId(int id)
    {
        _id = id;
        return this;
    }

    public VotoFactory ConHashes(string previo, string actual)
    {
        _hashPrevio = previo;
        _hashActual = actual;
        return this;
    }

    public VotoFactory ConFecha(DateTime fecha)
    {
        _fechaVoto = fecha;
        return this;
    }

    public VotoFactory EnUbicacion(int ubicacionId)
    {
        _ubicacionId = ubicacionId;
        return this;
    }

    public VotoFactory EnRecinto(int recintoId)
    {
        _recintoId = recintoId;
        return this;
    }

    public Voto Build()
    {
        return new Voto
        {
            Id = _id,
            EleccionId = _eleccionId,
            CandidatoId = _candidatoId,
            ListaId = _listaId,
            FechaVotoUtc = _fechaVoto,
            HashPrevio = _hashPrevio,
            HashActual = _hashActual,
            UbicacionId = _ubicacionId,
            RecintoId = _recintoId
        };
    }
}
