using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using SistemaVoto.Api.Controllers;
using SistemaVoto.Modelos;
using SistemaVoto.Tests.Factories;
using SistemaVoto.Tests.Helpers;
using Xunit;

namespace SistemaVoto.Tests.Controllers;

/// <summary>
/// Pruebas para EleccionesController usando el patrón Factory
/// </summary>
public class EleccionesControllerTests
{
    [Fact]
    public async Task List_DeberiaRetornarEleccionesPaginadas()
    {
        // Arrange - Usando Factory para crear contexto con datos
        using var context = DbContextFactory.CreateInMemoryContext("Test_List");
        
        // Crear elecciones usando Factory
        var elecciones = new[]
        {
            EleccionFactory.Nominal().ConId(1).ConTitulo("Elección 1").Build(),
            EleccionFactory.Plancha(10).ConId(2).ConTitulo("Elección 2").Build(),
            EleccionFactory.Activa().ConId(3).ConTitulo("Elección 3").Build()
        };
        
        context.Elecciones.AddRange(elecciones);
        await context.SaveChangesAsync();

        var controller = new EleccionesController(context);

        // Act
        var result = await controller.List(page: 1, pageSize: 10);

        // Assert - Usando FluentAssertions
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResult = okResult.Value.Should().BeAssignableTo<ApiResult<object>>().Subject;
        
        apiResult.Success.Should().BeTrue();
        apiResult.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ConDatosValidos_DeberiaCrearEleccion()
    {
        // Arrange
        using var context = DbContextFactory.CreateInMemoryContext("Test_Create");
        var controller = new EleccionesController(context);

        var request = new Api.Dtos.CreateEleccionRequest
        {
            Titulo = "Nueva Elección",
            Descripcion = "Descripción de prueba",
            FechaInicioUtc = DateTime.UtcNow.AddDays(1),
            FechaFinUtc = DateTime.UtcNow.AddDays(8),
            Tipo = TipoEleccion.Nominal,
            NumEscanos = 0,
            UsaUbicacion = false,
            ModoUbicacion = ModoUbicacion.Ninguna
        };

        // Act
        var result = await controller.Create(request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResult = okResult.Value.Should().BeAssignableTo<ApiResult<object>>().Subject;
        
        apiResult.Success.Should().BeTrue();
        
        // Verificar que se creó en la base de datos
        var eleccionCreada = await context.Elecciones.FindAsync(1);
        eleccionCreada.Should().NotBeNull();
        eleccionCreada!.Titulo.Should().Be("Nueva Elección");
    }

    [Fact]
    public async Task Create_ConFechasInvalidas_DeberiaRetornarBadRequest()
    {
        // Arrange
        using var context = DbContextFactory.CreateInMemoryContext("Test_Create_Invalid");
        var controller = new EleccionesController(context);

        var request = new Api.Dtos.CreateEleccionRequest
        {
            Titulo = "Elección Inválida",
            FechaInicioUtc = DateTime.UtcNow.AddDays(8), // Fin antes de inicio
            FechaFinUtc = DateTime.UtcNow.AddDays(1),
            Tipo = TipoEleccion.Nominal,
            NumEscanos = 0,
            UsaUbicacion = false,
            ModoUbicacion = ModoUbicacion.Ninguna
        };

        // Act
        var result = await controller.Create(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_TipoNominalConEscanos_DeberiaRetornarBadRequest()
    {
        // Arrange
        using var context = DbContextFactory.CreateInMemoryContext("Test_Create_NominalEscanos");
        var controller = new EleccionesController(context);

        var request = new Api.Dtos.CreateEleccionRequest
        {
            Titulo = "Elección Inválida",
            FechaInicioUtc = DateTime.UtcNow.AddDays(1),
            FechaFinUtc = DateTime.UtcNow.AddDays(8),
            Tipo = TipoEleccion.Nominal,
            NumEscanos = 10, // ? Nominal no debe tener escaños
            UsaUbicacion = false,
            ModoUbicacion = ModoUbicacion.Ninguna
        };

        // Act
        var result = await controller.Create(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result.Result as BadRequestObjectResult;
        var apiResult = badRequest!.Value as ApiResult<object>;
        apiResult!.Success.Should().BeFalse();
        apiResult.Message.Should().Contain("NumEscanos debe ser 0");
    }

    [Fact]
    public async Task Conteo_EleccionNominal_DeberiaRetornarResultados()
    {
        // Arrange - Usando Factory para crear escenario completo
        using var context = DbContextFactory.CreateInMemoryContext("Test_Conteo");
        
        // Crear elección nominal
        var eleccion = EleccionFactory.Nominal()
            .ConId(1)
            .ConTitulo("Elección Presidencial")
            .Build();
        context.Elecciones.Add(eleccion);

        // Crear candidatos usando Factory
        var candidato1 = CandidatoFactory.ParaEleccion(1)
            .ConId(1)
            .ConNombre("Candidato A")
            .Build();
        
        var candidato2 = CandidatoFactory.ParaEleccion(1)
            .ConId(2)
            .ConNombre("Candidato B")
            .Build();
        
        context.Candidatos.AddRange(candidato1, candidato2);

        // Crear votos usando Factory
        var votos = new[]
        {
            VotoFactory.ParaEleccion(1).ConId(1).ParaCandidato(1).Build(),
            VotoFactory.ParaEleccion(1).ConId(2).ParaCandidato(1).Build(),
            VotoFactory.ParaEleccion(1).ConId(3).ParaCandidato(2).Build()
        };
        context.Votos.AddRange(votos);
        
        await context.SaveChangesAsync();

        var controller = new EleccionesController(context);

        // Act
        var result = await controller.Conteo(id: 1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        // Verificar estructura de respuesta usando reflection
        var data = okResult.Value;
        var categorias = data!.GetType().GetProperty("categorias")!.GetValue(data) as List<string>;
        var votos2 = data.GetType().GetProperty("votos")!.GetValue(data) as List<int>;
        
        categorias.Should().HaveCount(2);
        votos2.Should().HaveCount(2);
        votos2.Should().ContainInOrder(2, 1); // Candidato A: 2 votos, Candidato B: 1 voto
    }
}
