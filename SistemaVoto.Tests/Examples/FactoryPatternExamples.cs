using FluentAssertions;
using SistemaVoto.Tests.Factories;
using Xunit;

namespace SistemaVoto.Tests.Examples;

/// <summary>
/// Ejemplos de uso del patrón Factory en diferentes escenarios
/// </summary>
public class FactoryPatternExamples
{
    /// <summary>
    /// Ejemplo 1: Crear objetos simples
    /// </summary>
    [Fact]
    public void Ejemplo1_CrearEleccionSimple()
    {
        // Crear una elección nominal con valores por defecto
        var eleccion = EleccionFactory.Nominal().Build();

        // Verificar
        eleccion.Should().NotBeNull();
        eleccion.Tipo.Should().Be(Modelos.TipoEleccion.Nominal);
        eleccion.NumEscanos.Should().Be(0);
    }

    /// <summary>
    /// Ejemplo 2: Personalizar con Fluent Interface
    /// </summary>
    [Fact]
    public void Ejemplo2_PersonalizarConFluentInterface()
    {
        // Encadenar métodos para personalizar
        var eleccion = EleccionFactory.Plancha(escanos: 15)
            .ConId(100)
            .ConTitulo("Elección Legislativa Nacional")
            .ConDescripcion("Elección para renovar el Congreso")
            .ConFechas(
                inicio: new DateTime(2024, 10, 1),
                fin: new DateTime(2024, 10, 2)
            )
            .ConEstado(Modelos.EstadoEleccion.Activa)
            .Build();

        // Verificar
        eleccion.Id.Should().Be(100);
        eleccion.Titulo.Should().Be("Elección Legislativa Nacional");
        eleccion.NumEscanos.Should().Be(15);
        eleccion.Estado.Should().Be(Modelos.EstadoEleccion.Activa);
    }

    /// <summary>
    /// Ejemplo 3: Crear múltiples objetos con variaciones
    /// </summary>
    [Fact]
    public void Ejemplo3_CrearMultiplesObjetos()
    {
        // Crear 5 candidatos diferentes
        var candidatos = new[]
        {
            CandidatoFactory.ParaEleccion(1).ConId(1).ConNombre("Candidato A").Build(),
            CandidatoFactory.ParaEleccion(1).ConId(2).ConNombre("Candidato B").Build(),
            CandidatoFactory.ParaEleccion(1).ConId(3).ConNombre("Candidato C").Build(),
            CandidatoFactory.ParaEleccion(1).ConId(4).ConNombre("Candidato D").Build(),
            CandidatoFactory.ParaEleccion(1).ConId(5).ConNombre("Candidato E").Build()
        };

        // Verificar
        candidatos.Should().HaveCount(5);
        candidatos.Select(c => c.Nombre).Should().OnlyHaveUniqueItems();
    }

    /// <summary>
    /// Ejemplo 4: Combinar factories para crear escenarios complejos
    /// </summary>
    [Fact]
    public void Ejemplo4_EscenarioComplejo()
    {
        // Crear una elección
        var eleccion = EleccionFactory.Activa()
            .ConId(1)
            .ConTitulo("Elección Presidencial")
            .Build();

        // Crear candidatos para esa elección
        var candidato1 = CandidatoFactory.ParaEleccion(eleccion.Id)
            .ConId(1)
            .ConNombre("María González")
            .ConPartido("Partido Progresista")
            .Build();

        var candidato2 = CandidatoFactory.ParaEleccion(eleccion.Id)
            .ConId(2)
            .ConNombre("Carlos Rodríguez")
            .ConPartido("Partido Conservador")
            .Build();

        // Crear votos para esos candidatos
        var votos = new[]
        {
            VotoFactory.ParaEleccion(eleccion.Id).ParaCandidato(candidato1.Id).Build(),
            VotoFactory.ParaEleccion(eleccion.Id).ParaCandidato(candidato1.Id).Build(),
            VotoFactory.ParaEleccion(eleccion.Id).ParaCandidato(candidato2.Id).Build()
        };

        // Verificar el escenario completo
        eleccion.Should().NotBeNull();
        candidato1.EleccionId.Should().Be(eleccion.Id);
        candidato2.EleccionId.Should().Be(eleccion.Id);
        votos.Should().HaveCount(3);
        votos.Count(v => v.CandidatoId == candidato1.Id).Should().Be(2);
        votos.Count(v => v.CandidatoId == candidato2.Id).Should().Be(1);
    }

    /// <summary>
    /// Ejemplo 5: Crear datos para probar edge cases
    /// </summary>
    [Fact]
    public void Ejemplo5_EdgeCases()
    {
        // Elección sin escaños (caso límite válido para Nominal)
        var eleccionNominal = EleccionFactory.Nominal()
            .ConEscanos(0)
            .Build();
        eleccionNominal.NumEscanos.Should().Be(0);

        // Elección con muchos escaños
        var eleccionGrande = EleccionFactory.Plancha(escanos: 500)
            .Build();
        eleccionGrande.NumEscanos.Should().Be(500);

        // Elección finalizada hace mucho tiempo
        var eleccionHistorica = EleccionFactory.Cerrada()
            .ConFechas(
                inicio: new DateTime(1990, 1, 1),
                fin: new DateTime(1990, 1, 2)
            )
            .Build();
        eleccionHistorica.FechaFinUtc.Year.Should().Be(1990);
    }

    /// <summary>
    /// Ejemplo 6: Reutilizar configuración base
    /// </summary>
    [Fact]
    public void Ejemplo6_ReutilizarConfiguracion()
    {
        // Crear una configuración base
        var baseFactory = EleccionFactory.Plancha(10)
            .ConTitulo("Elección Base")
            .ConFechas(DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        // Crear variaciones de la misma base
        var eleccion1 = baseFactory.ConId(1).ConEstado(Modelos.EstadoEleccion.Pendiente).Build();
        var eleccion2 = baseFactory.ConId(2).ConEstado(Modelos.EstadoEleccion.Activa).Build();
        var eleccion3 = baseFactory.ConId(3).ConEstado(Modelos.EstadoEleccion.Cerrada).Build();

        // Todas comparten configuración base pero con diferentes estados
        eleccion1.Titulo.Should().Be("Elección Base");
        eleccion2.Titulo.Should().Be("Elección Base");
        eleccion3.Titulo.Should().Be("Elección Base");

        eleccion1.Estado.Should().Be(Modelos.EstadoEleccion.Pendiente);
        eleccion2.Estado.Should().Be(Modelos.EstadoEleccion.Activa);
        eleccion3.Estado.Should().Be(Modelos.EstadoEleccion.Cerrada);           
    }

    /// <summary>
    /// Ejemplo 7: Generar datos para pruebas de performance
    /// </summary>
    [Fact]
    public void Ejemplo7_DatosParaPerformance()
    {
        // Generar 1000 votos rápidamente
        var votos = Enumerable.Range(1, 1000)
            .Select(i => VotoFactory.ParaEleccion(1)
                .ParaCandidato(i % 5 + 1) // Distribuir entre 5 candidatos
                .ConId(i)
                .Build())
            .ToList();

        // Verificar
        votos.Should().HaveCount(1000);
        
        // Contar votos por candidato
        var votosPorCandidato = votos
            .GroupBy(v => v.CandidatoId)
            .ToDictionary(g => g.Key!.Value, g => g.Count());

        votosPorCandidato.Should().HaveCount(5);
        votosPorCandidato.Values.Sum().Should().Be(1000);
    }
}
