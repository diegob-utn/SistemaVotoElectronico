using Microsoft.EntityFrameworkCore;
using SistemaVoto.Api.Data;

namespace SistemaVoto.Tests.Helpers;

/// <summary>
/// Helper para crear DbContext en memoria para pruebas
/// Patrón Factory para contextos de prueba
/// </summary>
public static class DbContextFactory
{
    /// <summary>
    /// Crea un contexto de base de datos en memoria para pruebas
    /// </summary>
    public static SistemaVotoDbContext CreateInMemoryContext(string databaseName = "TestDb")
    {
        var options = new DbContextOptionsBuilder<SistemaVotoDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        var context = new SistemaVotoDbContext(options);
        context.Database.EnsureCreated();
        
        return context;
    }

    /// <summary>
    /// Crea un contexto con datos de prueba precargados
    /// </summary>
    public static SistemaVotoDbContext CreateSeededContext(string databaseName = "TestDb")
    {
        var context = CreateInMemoryContext(databaseName);
        SeedTestData(context);
        return context;
    }

    /// <summary>
    /// Carga datos de prueba estándar en el contexto
    /// </summary>
    private static void SeedTestData(SistemaVotoDbContext context)
    {
        // Elecciones de prueba
        var eleccion1 = Factories.EleccionFactory.Nominal()
            .ConId(1)
            .ConTitulo("Elección Presidencial 2024")
            .Build();

        var eleccion2 = Factories.EleccionFactory.Plancha(10)
            .ConId(2)
            .ConTitulo("Elección Legislativa 2024")
            .Build();

        context.Elecciones.AddRange(eleccion1, eleccion2);

        // Candidatos de prueba
        var candidato1 = Factories.CandidatoFactory.ParaEleccion(1)
            .ConId(1)
            .ConNombre("María González")
            .ConPartido("Partido A")
            .Build();

        var candidato2 = Factories.CandidatoFactory.ParaEleccion(1)
            .ConId(2)
            .ConNombre("Carlos Rodríguez")
            .ConPartido("Partido B")
            .Build();

        context.Candidatos.AddRange(candidato1, candidato2);

        context.SaveChanges();
    }
}
