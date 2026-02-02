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
    /// Genera la tabla detallada del proceso D'Hondt
    /// </summary>
    public List<TablaDistribucionRow> GenerarTablaDHondt(
        List<(string Nombre, int Votos)> votos, 
        int escanos)
    {
        var tabla = new List<TablaDistribucionRow>();
        
        foreach (var (nombre, votosPartido) in votos)
        {
            var row = new TablaDistribucionRow
            {
                Nombre = nombre,
                VotosTotales = votosPartido,
                Divisores = new List<double>()
            };
            
            // Calcular cocientes para cada divisor (1, 2, 3, ..., escanos)
            for (int d = 1; d <= escanos; d++)
            {
                row.Divisores.Add((double)votosPartido / d);
            }
            
            tabla.Add(row);
        }
        
        return tabla;
    }
}

/// <summary>
/// Fila para la tabla de distribucion de escanos
/// </summary>
public class TablaDistribucionRow
{
    public string Nombre { get; set; } = null!;
    public int VotosTotales { get; set; }
    public List<double> Divisores { get; set; } = new();
    public int EscanosAsignados { get; set; }
}
