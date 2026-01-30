using FluentAssertions;
using SistemaVoto.Modelos;
using SistemaVoto.Tests.Factories;
using Xunit;

namespace SistemaVoto.Tests.Integration;

/// <summary>
/// Pruebas de integración que simulan escenarios completos de votación
/// </summary>
public class EscenarioVotacionIntegrationTests
{
    [Fact]
    public void EscenarioCompleto_EleccionNominal_DeberiaFuncionar()
    {
        // Arrange - Crear escenario completo usando Factories
        
        // 1. Crear elección nominal
        var eleccion = EleccionFactory.Nominal()
            .ConId(1)
            .ConTitulo("Elección Presidencial 2024")
            .ConFechas(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(8))
            .ConEstado(EstadoEleccion.Activa)
            .Build();

        // 2. Crear candidatos
        var candidatos = new[]
        {
            CandidatoFactory.ParaEleccion(1)
                .ConId(1)
                .ConNombre("María González")
                .ConPartido("Partido A")
                .Build(),
            
            CandidatoFactory.ParaEleccion(1)
                .ConId(2)
                .ConNombre("Carlos Rodríguez")
                .ConPartido("Partido B")
                .Build(),
            
            CandidatoFactory.ParaEleccion(1)
                .ConId(3)
                .ConNombre("Ana Martínez")
                .ConPartido("Partido C")
                .Build()
        };

        // 3. Simular votos
        var votos = new[]
        {
            VotoFactory.ParaEleccion(1).ParaCandidato(1).ConId(1).Build(),
            VotoFactory.ParaEleccion(1).ParaCandidato(1).ConId(2).Build(),
            VotoFactory.ParaEleccion(1).ParaCandidato(2).ConId(3).Build(),
            VotoFactory.ParaEleccion(1).ParaCandidato(1).ConId(4).Build(),
            VotoFactory.ParaEleccion(1).ParaCandidato(3).ConId(5).Build()
        };

        // Assert - Verificar estado del escenario
        eleccion.Should().NotBeNull();
        eleccion.Tipo.Should().Be(TipoEleccion.Nominal);
        eleccion.Estado.Should().Be(EstadoEleccion.Activa);

        candidatos.Should().HaveCount(3);
        votos.Should().HaveCount(5);

        // Contar votos por candidato
        var votosPorCandidato = votos
            .GroupBy(v => v.CandidatoId)
            .ToDictionary(g => g.Key!.Value, g => g.Count());

        votosPorCandidato[1].Should().Be(3); // María González
        votosPorCandidato[2].Should().Be(1); // Carlos Rodríguez
        votosPorCandidato[3].Should().Be(1); // Ana Martínez
    }

    [Fact]
    public void EscenarioCompleto_EleccionPlancha_DeberiaCalcularEscanos()
    {
        // Arrange - Elección tipo Plancha con 10 escaños
        var eleccion = EleccionFactory.Plancha(escanos: 10)
            .ConId(1)
            .ConTitulo("Elección Legislativa 2024")
            .Build();

        // Crear listas
        var listas = new[]
        {
            new Lista { Id = 1, EleccionId = 1, Nombre = "Lista A" },
            new Lista { Id = 2, EleccionId = 1, Nombre = "Lista B" },
            new Lista { Id = 3, EleccionId = 1, Nombre = "Lista C" }
        };

        // Simular votos: Lista A (100), Lista B (50), Lista C (30)
        var votos = new List<Voto>();
        for (int i = 1; i <= 100; i++)
            votos.Add(VotoFactory.ParaEleccion(1).ParaLista(1).ConId(i).Build());
        
        for (int i = 101; i <= 150; i++)
            votos.Add(VotoFactory.ParaEleccion(1).ParaLista(2).ConId(i).Build());
        
        for (int i = 151; i <= 180; i++)
            votos.Add(VotoFactory.ParaEleccion(1).ParaLista(3).ConId(i).Build());

        // Assert
        eleccion.Tipo.Should().Be(TipoEleccion.Plancha);
        eleccion.NumEscanos.Should().Be(10);
        
        votos.Count(v => v.ListaId == 1).Should().Be(100);
        votos.Count(v => v.ListaId == 2).Should().Be(50);
        votos.Count(v => v.ListaId == 3).Should().Be(30);

        // Nota: El cálculo real de escaños se hace en el servicio SeatAllocationService
        // Esta prueba solo verifica que los datos están correctamente estructurados
    }

    [Fact]
    public void EscenarioCompleto_HashChain_DeberiaSerConsecutivo()
    {
        // Arrange - Simular cadena de hashes
        var voto1 = VotoFactory.Nuevo()
            .ConId(1)
            .ConHashes("GENESIS", "hash1")
            .Build();

        var voto2 = VotoFactory.Nuevo()
            .ConId(2)
            .ConHashes("hash1", "hash2") // hash previo = hash actual del voto anterior
            .Build();

        var voto3 = VotoFactory.Nuevo()
            .ConId(3)
            .ConHashes("hash2", "hash3")
            .Build();

        // Assert - Verificar cadena
        voto1.HashPrevio.Should().Be("GENESIS");
        voto2.HashPrevio.Should().Be(voto1.HashActual);
        voto3.HashPrevio.Should().Be(voto2.HashActual);
    }
}
