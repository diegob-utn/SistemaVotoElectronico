using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using SistemaVoto.Api.Controllers;
using SistemaVoto.Api.Dtos;
using SistemaVoto.Api.Hubs;
using SistemaVoto.Modelos;
using SistemaVoto.Tests.Factories;
using SistemaVoto.Tests.Helpers;
using Xunit;

namespace SistemaVoto.Tests.Controllers;

/// <summary>
/// Pruebas para VotosController usando el patrón Factory
/// </summary>
public class VotosControllerTests
{
    [Fact]
    public async Task Votar_ConDatosValidos_DeberiaRegistrarVoto()
    {
        // Arrange - Usando Factory
        using var context = DbContextFactory.CreateInMemoryContext("Test_Votar");
        
        // Crear elección nominal activa
        var eleccion = EleccionFactory.Nominal()
            .ConId(1)
            .ConEstado(EstadoEleccion.Activa)
            .Build();
        context.Elecciones.Add(eleccion);

        // Crear candidato
        var candidato = CandidatoFactory.ParaEleccion(1)
            .ConId(5)
            .ConNombre("Juan Pérez")
            .Build();
        context.Candidatos.Add(candidato);
        
        await context.SaveChangesAsync();

        // Mock de SignalR Hub
        var mockHubContext = new Mock<IHubContext<VotacionHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        
        mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);
        mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

        var controller = new VotosController(context, mockHubContext.Object);

        var request = new VotarRequest
        {
            UsuarioId = 123,
            CandidatoId = 5,
            ListaId = null,
            UbicacionId = null,
            RecintoId = null
        };

        // Act
        var result = await controller.Votar(eleccionId: 1, req: request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResult = okResult.Value.Should().BeAssignableTo<ApiResult<object>>().Subject;
        
        apiResult.Success.Should().BeTrue();

        // Verificar que se guardó el voto
        var voto = context.Votos.FirstOrDefault();
        voto.Should().NotBeNull();
        voto!.CandidatoId.Should().Be(5);
        voto.EleccionId.Should().Be(1);

        // Verificar historial
        var historial = context.HistorialVotaciones.FirstOrDefault();
        historial.Should().NotBeNull();
        historial!.UsuarioId.Should().Be(123);
    }

    [Fact]
    public async Task Votar_UsuarioYaVoto_DeberiaRetornarBadRequest()
    {
        // Arrange
        using var context = DbContextFactory.CreateInMemoryContext("Test_Votar_Duplicado");
        
        var eleccion = EleccionFactory.Nominal().ConId(1).Build();
        context.Elecciones.Add(eleccion);

        var candidato = CandidatoFactory.ParaEleccion(1).ConId(5).Build();
        context.Candidatos.Add(candidato);

        // Usuario ya votó (historial existente)
        var historialPrevio = new HistorialVotacion
        {
            Id = 1,
            EleccionId = 1,
            UsuarioId = 123,
            FechaParticipacionUtc = DateTime.UtcNow.AddHours(-1),
            HashTransaccion = "hash123"
        };
        context.HistorialVotaciones.Add(historialPrevio);
        
        await context.SaveChangesAsync();

        var mockHubContext = new Mock<IHubContext<VotacionHub>>();
        var controller = new VotosController(context, mockHubContext.Object);

        var request = new VotarRequest
        {
            UsuarioId = 123, // Mismo usuario
            CandidatoId = 5
        };

        // Act
        var result = await controller.Votar(1, request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result.Result as BadRequestObjectResult;
        var apiResult = badRequest!.Value as ApiResult<object>;
        apiResult!.Message.Should().Contain("ya votó");
    }

    [Fact]
    public async Task Votar_CandidatoNoExiste_DeberiaRetornarBadRequest()
    {
        // Arrange
        using var context = DbContextFactory.CreateInMemoryContext("Test_Votar_CandidatoInvalido");
        
        var eleccion = EleccionFactory.Nominal().ConId(1).Build();
        context.Elecciones.Add(eleccion);
        await context.SaveChangesAsync();

        var mockHubContext = new Mock<IHubContext<VotacionHub>>();
        var controller = new VotosController(context, mockHubContext.Object);

        var request = new VotarRequest
        {
            UsuarioId = 123,
            CandidatoId = 999 // No existe
        };

        // Act
        var result = await controller.Votar(1, request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Votar_EnviarCandidatoYLista_DeberiaRetornarBadRequest()
    {
        // Arrange
        using var context = DbContextFactory.CreateInMemoryContext("Test_Votar_XOR");
        
        var eleccion = EleccionFactory.Nominal().ConId(1).Build();
        context.Elecciones.Add(eleccion);
        await context.SaveChangesAsync();

        var mockHubContext = new Mock<IHubContext<VotacionHub>>();
        var controller = new VotosController(context, mockHubContext.Object);

        var request = new VotarRequest
        {
            UsuarioId = 123,
            CandidatoId = 5,
            ListaId = 10 // ? No puede enviar ambos
        };

        // Act
        var result = await controller.Votar(1, request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result.Result as BadRequestObjectResult;
        var apiResult = badRequest!.Value as ApiResult<object>;
        apiResult!.Message.Should().Contain("exactamente uno");
    }
}
