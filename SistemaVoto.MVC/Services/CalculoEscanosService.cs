namespace SistemaVoto.MVC.Services;

/// <summary>
/// Servicio para calculo de escanos usando metodos D'Hondt y Webster
/// </summary>
public class CalculoEscanosService
{
    /// <summary>
    /// Calcula la distribucion de escanos usando el metodo D'Hondt
    /// </summary>
    /// <param name="votos">Lista de tuplas (nombre partido/candidato, votos obtenidos)</param>
    /// <param name="escanos">Numero total de escanos a distribuir</param>
    /// <returns>Lista de tuplas (nombre, escanos asignados)</returns>
    public List<(string Nombre, int Escanos)> CalcularDHondt(List<(string Nombre, int Votos)> votos, int escanos)
    {
        if (votos == null || !votos.Any() || escanos <= 0)
            return new List<(string, int)>();

        // Inicializar resultado
        var resultado = votos.Select(v => (v.Nombre, Escanos: 0)).ToList();
        var indices = new Dictionary<string, int>();
        for (int i = 0; i < votos.Count; i++)
            indices[votos[i].Nombre] = i;

        // Asignar escanos uno por uno
        for (int e = 0; e < escanos; e++)
        {
            int maxIndex = 0;
            double maxCociente = 0;

            for (int i = 0; i < votos.Count; i++)
            {
                // Cociente D'Hondt: Votos / (Escanos ya asignados + 1)
                double cociente = (double)votos[i].Votos / (resultado[i].Escanos + 1);
                
                if (cociente > maxCociente)
                {
                    maxCociente = cociente;
                    maxIndex = i;
                }
            }

            resultado[maxIndex] = (resultado[maxIndex].Nombre, resultado[maxIndex].Escanos + 1);
        }

        return resultado.OrderByDescending(r => r.Escanos).ToList();
    }

    /// <summary>
    /// Calcula la distribucion de escanos usando el metodo Webster (Sainte-Lague)
    /// </summary>
    /// <param name="votos">Lista de tuplas (nombre partido/candidato, votos obtenidos)</param>
    /// <param name="escanos">Numero total de escanos a distribuir</param>
    /// <returns>Lista de tuplas (nombre, escanos asignados)</returns>
    public List<(string Nombre, int Escanos)> CalcularWebster(List<(string Nombre, int Votos)> votos, int escanos)
    {
        if (votos == null || !votos.Any() || escanos <= 0)
            return new List<(string, int)>();

        // Inicializar resultado
        var resultado = votos.Select(v => (v.Nombre, Escanos: 0)).ToList();

        // Asignar escanos uno por uno
        for (int e = 0; e < escanos; e++)
        {
            int maxIndex = 0;
            double maxCociente = 0;

            for (int i = 0; i < votos.Count; i++)
            {
                // Cociente Webster: Votos / (2 * Escanos ya asignados + 1)
                double cociente = (double)votos[i].Votos / (2 * resultado[i].Escanos + 1);
                
                if (cociente > maxCociente)
                {
                    maxCociente = cociente;
                    maxIndex = i;
                }
            }

            resultado[maxIndex] = (resultado[maxIndex].Nombre, resultado[maxIndex].Escanos + 1);
        }

        return resultado.OrderByDescending(r => r.Escanos).ToList();
    }

    /// <summary>
    /// Calcula el detalle completo de asignacion D'Hondt incluyendo tabla de cocientes
    /// </summary>
    public DetalleAsignacion CalcularDetalleDHondt(List<(string Nombre, int Votos)> votos, int escanos)
    {
        return CalcularDetalleGenerico(votos, escanos, "D'Hondt", i => i + 1);
    }

    /// <summary>
    /// Calcula el detalle completo de asignacion Webster incluyendo tabla de cocientes
    /// </summary>
    public DetalleAsignacion CalcularDetalleWebster(List<(string Nombre, int Votos)> votos, int escanos)
    {
        return CalcularDetalleGenerico(votos, escanos, "Webster", i => 2 * i + 1);
    }

    private DetalleAsignacion CalcularDetalleGenerico(
        List<(string Nombre, int Votos)> votos, 
        int escanos, 
        string nombreMetodo,
        Func<int, double> divisorFunc)
    {
        var detalle = new DetalleAsignacion
        {
            Metodo = nombreMetodo,
            EscanosTotales = escanos
        };

        if (votos == null || !votos.Any() || escanos <= 0)
            return detalle;

        // 1. Generar todos los cocientes posibles
        var todosCocientes = new List<Cociente>();

        foreach (var (nombre, votosPartido) in votos)
        {
            var fila = new FilaAsignacion { Partido = nombre, Votos = votosPartido };
            
            // Generamos cocientes hasta el numero de escanos (garantiza suficiencia)
            // O al menos un numero razonable para mostrar en tabla
            for (int i = 0; i < escanos; i++)
            {
                double divisor = divisorFunc(i);
                double valor = votosPartido / divisor;
                
                var cociente = new Cociente
                {
                    Partido = nombre,
                    VotosBase = votosPartido,
                    Divisor = divisor,
                    Valor = valor
                };
                
                fila.ColCocientes.Add(cociente);
                todosCocientes.Add(cociente);
            }
            detalle.Filas.Add(fila);
        }

        // 2. Ordenar cocientes descendente y tomar los N ganadores
        // Regla de desempate: Mayor voto total (implícito si el sort es estable, o forzarlo)
        var cocientesGanadores = todosCocientes
            .OrderByDescending(c => c.Valor)
            .ThenByDescending(c => c.VotosBase) // Desempate por votos totales
            .Take(escanos)
            .ToList();

        // 3. Marcar ganadores y asignar orden
        for (int i = 0; i < cocientesGanadores.Count; i++)
        {
            cocientesGanadores[i].EsGanador = true;
            cocientesGanadores[i].OrdenAsignacion = i + 1;
            
            // Actualizar conteo en fila
            var fila = detalle.Filas.First(f => f.Partido == cocientesGanadores[i].Partido);
            fila.EscanosTotales++;
        }

        detalle.CocientesGanadores = cocientesGanadores;
        
        // Ordenar filas por votos totales descendente para visualizacion
        detalle.Filas = detalle.Filas.OrderByDescending(f => f.Votos).ToList();

        return detalle;
    }
}

public class DetalleAsignacion
{
    public string Metodo { get; set; } = null!;
    public int EscanosTotales { get; set; }
    public List<FilaAsignacion> Filas { get; set; } = new();
    public List<Cociente> CocientesGanadores { get; set; } = new();
}

public class FilaAsignacion
{
    public string Partido { get; set; } = null!;
    public int Votos { get; set; }
    public List<Cociente> ColCocientes { get; set; } = new();
    public int EscanosTotales { get; set; }
}

public class Cociente
{
    public string Partido { get; set; } = null!;
    public int VotosBase { get; set; }
    public double Valor { get; set; }
    public double Divisor { get; set; }
    public bool EsGanador { get; set; }
    public int OrdenAsignacion { get; set; }
}
