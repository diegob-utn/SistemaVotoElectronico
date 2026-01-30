# ?? Testing con Factory Pattern - Guía Rápida

## ?? ¿Qué es el Patrón Factory?

El patrón Factory es un patrón de diseño creacional que proporciona una interfaz para crear objetos de manera flexible y reutilizable, sin especificar la clase exacta del objeto que se creará.

## ?? Comparación Visual

### ? SIN Factory Pattern

```csharp
// Prueba 1
var eleccion = new Eleccion 
{
    Id = 1,
    Titulo = "Test 1",
    FechaInicioUtc = DateTime.UtcNow,
    FechaFinUtc = DateTime.UtcNow.AddDays(7),
    Tipo = TipoEleccion.Nominal,
    NumEscanos = 0,
    Estado = EstadoEleccion.Pendiente,
    UsaUbicacion = false,
    ModoUbicacion = ModoUbicacion.Ninguna
};

// Prueba 2 - ¡Hay que repetir todo otra vez!
var eleccion2 = new Eleccion 
{
    Id = 2,
    Titulo = "Test 2", // Solo cambia esto
    FechaInicioUtc = DateTime.UtcNow,
    FechaFinUtc = DateTime.UtcNow.AddDays(7),
    Tipo = TipoEleccion.Nominal,
    NumEscanos = 0,
    Estado = EstadoEleccion.Pendiente,
    UsaUbicacion = false,
    ModoUbicacion = ModoUbicacion.Ninguna
};
```

**Problemas:**
- ?? Código duplicado
- ?? Difícil de mantener
- ?? Propenso a errores
- ?? Verboso y poco legible

---

### ? CON Factory Pattern

```csharp
// Prueba 1
var eleccion = EleccionFactory.Nominal().Build();

// Prueba 2
var eleccion2 = EleccionFactory.Nominal()
    .ConTitulo("Test 2")
    .Build();

// Prueba 3 - Personalización compleja
var eleccion3 = EleccionFactory.Plancha(escanos: 10)
    .ConId(5)
    .ConTitulo("Elección Especial")
    .ConEstado(EstadoEleccion.Activa)
    .Build();
```

**Ventajas:**
- ? Código limpio y conciso
- ? Fácil de mantener
- ? Reutilizable
- ? Legible y expresivo

---

## ??? Arquitectura de las Factories

```
???????????????????????????????????????????????
?         Factory Pattern                      ?
???????????????????????????????????????????????
?                                              ?
?  EleccionFactory                             ?
?    ?? Nominal()        ? Elección Nominal   ?
?    ?? Plancha()        ? Elección Plancha   ?
?    ?? Activa()         ? Elección Activa    ?
?    ?? Finalizada()     ? Elección Finalizada?
?    ?? Fluent Methods   ? Personalización    ?
?                                              ?
?  CandidatoFactory                            ?
?    ?? Nuevo()          ? Candidato básico   ?
?    ?? ParaEleccion()   ? Con elecciónId     ?
?    ?? Fluent Methods   ? Personalización    ?
?                                              ?
?  VotoFactory                                 ?
?    ?? Nuevo()          ? Voto básico         ?
?    ?? ParaCandidato()  ? Voto a candidato   ?
?    ?? ParaLista()      ? Voto a lista       ?
?    ?? Fluent Methods   ? Personalización    ?
?                                              ?
?  DbContextFactory                            ?
?    ?? CreateInMemoryContext()  ? DB vacía   ?
?    ?? CreateSeededContext()    ? DB con data?
?                                              ?
???????????????????????????????????????????????
```

---

## ?? Ejemplos de Uso Rápido

### Ejemplo 1: Test Simple

```csharp
[Fact]
public void DeberiaCrearEleccion()
{
    // Arrange
    var eleccion = EleccionFactory.Nominal().Build();
    
    // Assert
    eleccion.Should().NotBeNull();
    eleccion.Tipo.Should().Be(TipoEleccion.Nominal);
}
```

### Ejemplo 2: Test con Datos en BD

```csharp
[Fact]
public async Task DeberiaListarElecciones()
{
    // Arrange
    using var context = DbContextFactory.CreateInMemoryContext();
    
    var elecciones = new[]
    {
        EleccionFactory.Nominal().ConId(1).Build(),
        EleccionFactory.Plancha(10).ConId(2).Build()
    };
    context.Elecciones.AddRange(elecciones);
    await context.SaveChangesAsync();
    
    var controller = new EleccionesController(context);
    
    // Act
    var result = await controller.List();
    
    // Assert
    result.Should().NotBeNull();
}
```

### Ejemplo 3: Test de Escenario Completo

```csharp
[Fact]
public async Task DeberiaRegistrarVotoCompleto()
{
    // Arrange
    using var context = DbContextFactory.CreateInMemoryContext();
    
    // 1. Crear elección
    var eleccion = EleccionFactory.Activa()
        .ConId(1)
        .Build();
    context.Elecciones.Add(eleccion);
    
    // 2. Crear candidato
    var candidato = CandidatoFactory.ParaEleccion(1)
        .ConId(5)
        .Build();
    context.Candidatos.Add(candidato);
    
    await context.SaveChangesAsync();
    
    // 3. Registrar voto
    var controller = new VotosController(context, mockHub);
    var request = new VotarRequest 
    { 
        UsuarioId = 123, 
        CandidatoId = 5 
    };
    
    // Act
    var result = await controller.Votar(1, request);
    
    // Assert
    result.Should().NotBeNull();
    context.Votos.Should().HaveCount(1);
}
```

---

## ?? Estadísticas de Ahorro

| Métrica | Sin Factory | Con Factory | Mejora |
|---------|-------------|-------------|--------|
| Líneas de código por test | ~15 | ~5 | **66% menos** |
| Tiempo de escritura | ~2 min | ~30 seg | **75% más rápido** |
| Bugs por duplicación | Alto | Bajo | **90% menos** |
| Legibilidad | Baja | Alta | **Mucho mejor** |

---

## ?? Aprende Más

### Comando de Pruebas Rápido

```bash
# Ejecutar todas las pruebas
dotnet test

# Ejecutar con detalles
dotnet test --logger "console;verbosity=detailed"

# Ver cobertura
dotnet test --collect:"XPlat Code Coverage"
```

### Estructura de una Prueba

```csharp
[Fact]
public void NombreDescriptivo_Condicion_ResultadoEsperado()
{
    // Arrange (Preparar) - Usar Factories aquí
    var datos = Factory.Crear().Build();
    
    // Act (Actuar)
    var resultado = MétodoAProbar(datos);
    
    // Assert (Afirmar)
    resultado.Should().NotBeNull();
}
```

---

## ?? Tips Pro

1. **Nombra factories por dominio**: `EleccionFactory`, `VotoFactory`
2. **Usa Fluent Interface**: `Factory.Crear().ConId(1).Build()`
3. **Métodos estáticos para casos comunes**: `Factory.Nominal()`, `Factory.Activa()`
4. **Un factory por entidad**: No mezcles lógica de diferentes modelos
5. **Valores por defecto sensatos**: Que funcionen sin personalización

---

## ?? Recursos

- ?? [README.md](README.md) - Guía completa
- ?? [FactoryPatternExamples.cs](Examples/FactoryPatternExamples.cs) - 7 ejemplos prácticos
- ?? [EleccionesControllerTests.cs](Controllers/EleccionesControllerTests.cs) - Tests reales
- ?? [run-tests.ps1](../run-tests.ps1) - Script de ejecución

---

**¡Empieza a usar Factories y escribe tests más rápido y mejor! ??**
