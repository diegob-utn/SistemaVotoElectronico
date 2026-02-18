using Microsoft.EntityFrameworkCore;
using SistemaVoto.Data.Data;
using SistemaVoto.Modelos;

namespace SistemaVoto.MVC.Services;

/// <summary>
/// Servicio en segundo plano para cerrar elecciones finalizadas
/// </summary>
public class ElectionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ElectionBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Revisar cada minuto

    public ElectionBackgroundService(IServiceProvider serviceProvider, ILogger<ElectionBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de Cierre Automático de Elecciones iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndCloseElectionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar el proceso de cierre automático.");
            }

            // Esperar el siguiente ciclo
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckAndCloseElectionsAsync(CancellationToken stoppingToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            try 
            {
                var context = scope.ServiceProvider.GetRequiredService<SistemaVotoDbContext>();
                
                // Buscar elecciones activas cuya fecha de fin ya pasó
                var now = DateTime.UtcNow;
                
                var eleccionesVencidas = await context.Elecciones
                    .Where(e => e.Estado == EstadoEleccion.Activa && e.FechaFinUtc <= now)
                    .ToListAsync(stoppingToken);

                if (eleccionesVencidas.Any())
                {
                    _logger.LogInformation($"Se encontraron {eleccionesVencidas.Count} elecciones vencidas para cerrar.");

                    foreach (var eleccion in eleccionesVencidas)
                    {
                        eleccion.Estado = EstadoEleccion.Cerrada;
                        _logger.LogInformation($"Cerrando elección: {eleccion.Titulo} (ID: {eleccion.Id})");
                    }

                    await context.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dentro de CheckAndCloseElectionsAsync");
            }
        }
    }
}
