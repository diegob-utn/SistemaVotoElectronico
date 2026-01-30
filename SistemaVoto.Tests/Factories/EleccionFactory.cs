using SistemaVoto.Modelos;

namespace SistemaVoto.Tests.Factories;

/// <summary>
/// Factory para crear objetos Eleccion con valores predeterminados para pruebas.
/// Implementa el patrón Factory con Fluent Interface.
/// </summary>
public class EleccionFactory
{
    private int _id = 1;
    private string _titulo = "Elección de Prueba";
    private string? _descripcion = "Descripción de prueba";
    private DateTime _fechaInicio = DateTime.UtcNow;
    private DateTime _fechaFin = DateTime.UtcNow.AddDays(7);
    private TipoEleccion _tipo = TipoEleccion.Nominal;
    private int _numEscanos = 0;
    private EstadoEleccion _estado = EstadoEleccion.Pendiente;
    private bool _usaUbicacion = false;
    private ModoUbicacion _modoUbicacion = ModoUbicacion.Ninguna;

    /// <summary>
    /// Crea una elección tipo Nominal (candidatos individuales)
    /// </summary>
    public static EleccionFactory Nominal()
    {
        return new EleccionFactory
        {
            _tipo = TipoEleccion.Nominal,
            _numEscanos = 0,
            _titulo = "Elección Nominal de Prueba"
        };
    }

    /// <summary>
    /// Crea una elección tipo Plancha (listas con escaños)
    /// </summary>
    public static EleccionFactory Plancha(int escanos = 10)
    {
        return new EleccionFactory
        {
            _tipo = TipoEleccion.Plancha,
            _numEscanos = escanos,
            _titulo = "Elección por Plancha de Prueba"
        };
    }

    /// <summary>
    /// Crea una elección activa (en curso)
    /// </summary>
    public static EleccionFactory Activa()
    {
        return new EleccionFactory
        {
            _estado = EstadoEleccion.Activa,
            _fechaInicio = DateTime.UtcNow.AddHours(-1),
            _fechaFin = DateTime.UtcNow.AddDays(1)
        };
    }

    /// <summary>
    /// Crea una elección finalizada
    /// </summary>
    public static EleccionFactory Cerrada()
    {
        return new EleccionFactory
        {
            _estado = EstadoEleccion.Cerrada,
            _fechaInicio = DateTime.UtcNow.AddDays(-7),
            _fechaFin = DateTime.UtcNow.AddDays(-1)
        };
    }

    // Métodos Fluent para personalizar
    public EleccionFactory ConId(int id)
    {
        _id = id;
        return this;
    }

    public EleccionFactory ConTitulo(string titulo)
    {
        _titulo = titulo;
        return this;
    }

    public EleccionFactory ConDescripcion(string? descripcion)
    {
        _descripcion = descripcion;
        return this;
    }

    public EleccionFactory ConFechas(DateTime inicio, DateTime fin)
    {
        _fechaInicio = inicio;
        _fechaFin = fin;
        return this;
    }

    public EleccionFactory ConEstado(EstadoEleccion estado)
    {
        _estado = estado;
        return this;
    }

    public EleccionFactory ConUbicaciones(ModoUbicacion modo)
    {
        _usaUbicacion = modo != ModoUbicacion.Ninguna;
        _modoUbicacion = modo;
        return this;
    }

    public EleccionFactory ConEscanos(int escanos)
    {
        _numEscanos = escanos;
        return this;
    }

    /// <summary>
    /// Construye el objeto Eleccion
    /// </summary>
    public Eleccion Build()
    {
        return new Eleccion
        {
            Id = _id,
            Titulo = _titulo,
            Descripcion = _descripcion,
            FechaInicioUtc = _fechaInicio,
            FechaFinUtc = _fechaFin,
            Tipo = _tipo,
            NumEscanos = _numEscanos,
            Estado = _estado,
            UsaUbicacion = _usaUbicacion,
            ModoUbicacion = _modoUbicacion
        };
    }
}
