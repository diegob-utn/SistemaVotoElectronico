# SistemaVoto.Tests - Guía de Pruebas con Factory Pattern

## ?? Patrón Factory Implementado

Este proyecto de pruebas implementa el **patrón Factory** para crear objetos de prueba de manera fácil, limpia y reutilizable.

## ?? Estructura del Proyecto

```
SistemaVoto.Tests/
??? Factories/
?   ??? EleccionFactory.cs      # Factory para Elecciones
?   ??? CandidatoFactory.cs     # Factory para Candidatos
?   ??? VotoFactory.cs          # Factory para Votos
??? Helpers/
?   ??? DbContextFactory.cs     # Factory para DbContext en memoria
??? Controllers/
?   ??? EleccionesControllerTests.cs
?   ??? VotosControllerTests.cs
??? Integration/
    ??? EscenarioVotacionIntegrationTests.cs
```

## ?? Cómo Usar las Factories

### 1. Factory de Elecciones

```csharp
// Crear elección nominal simple
var eleccion = EleccionFactory.Nominal().Build();

// Crear elección tipo plancha con escaños
var eleccion = EleccionFactory.Plancha(escanos: 10).Build();

// Crear elección activa con personalización
var eleccion = EleccionFactory.Activa()
    .ConId(5)
    .ConTitulo("Elección Presidencial 2024")
    .ConDescripcion("Descripción personalizada")
    .ConFechas(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(8))
    .Build();

// Crear elección finalizada
var eleccion = EleccionFactory.Finalizada()
    .ConTitulo("Elección Histórica")
    .Build();
```

### 2. Factory de Candidatos

```csharp
// Crear candidato simple
var candidato = CandidatoFactory.Nuevo().Build();

// Crear candidato para una elección específica
var candidato = CandidatoFactory.ParaEleccion(eleccionId: 1)
    .ConId(5)
    .ConNombre("María González")
    .ConPartido("Partido A")
    .ConPropuestas("Mejoras en educación y salud")
    .Build();

// Crear candidato de una lista (para elecciones tipo Plancha)
var candidato = CandidatoFactory.ParaEleccion(1)
    .ConNombre("Carlos Pérez")
    .DeLista(listaId: 3)
    .Build();
```

### 3. Factory de Votos

```csharp
// Crear voto para candidato
var voto = VotoFactory.ParaCandidato(candidatoId: 5)
    .ConId(1)
    .Build();

// Crear voto para lista
var voto = VotoFactory.ParaLista(listaId: 2)
    .ConId(10)
    .Build();

// Crear voto con ubicación
var voto = VotoFactory.ParaCandidato(5)
    .EnUbicacion(ubicacionId: 3)
    .ConFecha(DateTime.UtcNow.AddHours(-2))
    .Build();

// Crear voto con hash chain
var voto = VotoFactory.Nuevo()
    .ConHashes(previo: "hash1", actual: "hash2")
    .Build();
```

### 4. Factory de DbContext

```csharp
// Crear contexto vacío en memoria
using var context = DbContextFactory.CreateInMemoryContext("MiTestDb");

// Crear contexto con datos precargados
using var context = DbContextFactory.CreateSeededContext("TestConDatos");
// Ya tiene elecciones y candidatos listos para probar
```

## ?? Ejemplo de Prueba Completa

```csharp
[Fact]
public async Task Votar_ConDatosValidos_DeberiaRegistrarVoto()
{
    // Arrange - Crear escenario usando Factories
    using var context = DbContextFactory.CreateInMemoryContext("Test1");
    
    // Crear elección activa
    var eleccion = EleccionFactory.Activa()
        .ConId(1)
        .ConTitulo("Elección Presidencial")
        .Build();
    context.Elecciones.Add(eleccion);

    // Crear candidato
    var candidato = CandidatoFactory.ParaEleccion(1)
        .ConId(5)
        .ConNombre("Juan Pérez")
        .Build();
    context.Candidatos.Add(candidato);
    
    await context.SaveChangesAsync();

    var controller = new VotosController(context, mockHub);

    // Act
    var result = await controller.Votar(
        eleccionId: 1,
        new VotarRequest { UsuarioId = 123, CandidatoId = 5 }
    );

    // Assert
    result.Should().NotBeNull();
    var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
    
    // Verificar que se guardó
    var voto = context.Votos.FirstOrDefault();
    voto.Should().NotBeNull();
    voto!.CandidatoId.Should().Be(5);
}
```

## ?? Ejecutar las Pruebas

### Desde Visual Studio
1. Abre **Test Explorer** (Ctrl+E, T)
2. Haz clic en **Run All**

### Desde Línea de Comandos
```bash
# Ejecutar todas las pruebas
dotnet test

# Ejecutar con detalles
dotnet test --logger "console;verbosity=detailed"

# Ejecutar solo una clase de pruebas
dotnet test --filter "FullyQualifiedName~EleccionesControllerTests"

# Ejecutar solo una prueba específica
dotnet test --filter "FullyQualifiedName~List_DeberiaRetornarEleccionesPaginadas"
```

## ?? Ventajas del Patrón Factory

### ? Ventajas

1. **Código más limpio**: No repites `new Eleccion { ... }` en cada prueba
2. **Fácil mantenimiento**: Cambios en modelos solo se actualizan en factories
3. **Reutilizable**: Las mismas factories sirven para muchas pruebas
4. **Legible**: `EleccionFactory.Activa()` es más claro que crear objeto manual
5. **Fluent Interface**: Encadena métodos para personalizar fácilmente
6. **Datos consistentes**: Valores por defecto coherentes en todas las pruebas

### ?? Comparación

**Antes (sin Factory)**
```csharp
var eleccion = new Eleccion
{
    Id = 1,
    Titulo = "Test",
    Descripcion = "Test desc",
    FechaInicioUtc = DateTime.UtcNow.AddDays(1),
    FechaFinUtc = DateTime.UtcNow.AddDays(8),
    Tipo = TipoEleccion.Nominal,
    NumEscanos = 0,
    Estado = EstadoEleccion.Activa,
    UsaUbicacion = false,
    ModoUbicacion = ModoUbicacion.Ninguna
};
```

**Después (con Factory)**
```csharp
var eleccion = EleccionFactory.Activa().ConTitulo("Test").Build();
```

## ?? Tecnologías Utilizadas

- **xUnit**: Framework de pruebas
- **FluentAssertions**: Aserciones legibles y expresivas
- **Moq**: Mocking de dependencias (SignalR, servicios)
- **EF Core InMemory**: Base de datos en memoria para pruebas
- **Factory Pattern**: Creación de objetos de prueba

## ?? Recursos Adicionales

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions](https://fluentassertions.com/)
- [Moq Quickstart](https://github.com/moq/moq4)
- [Factory Pattern](https://refactoring.guru/design-patterns/factory-method)

## ?? Próximos Pasos

1. **Agregar más factories**: ListaFactory, UbicacionFactory, RecintoFactory
2. **Builder Pattern avanzado**: Para escenarios más complejos
3. **AutoFixture**: Para generación automática de datos
4. **Test Data Builders**: Patrón complementario para casos edge
5. **Snapshot Testing**: Para validar respuestas completas de API

---

**¡Feliz Testing! ???**
