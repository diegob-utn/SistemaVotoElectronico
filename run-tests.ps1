# Script para ejecutar pruebas del Sistema de Voto Electrónico

Write-Host "?? Sistema de Voto Electrónico - Test Runner" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Función para ejecutar comando con manejo de errores
function Run-TestCommand {
    param(
        [string]$Command,
        [string]$Description
    )
    
    Write-Host "??  $Description" -ForegroundColor Yellow
    Invoke-Expression $Command
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? $Description - OK" -ForegroundColor Green
    } else {
        Write-Host "? $Description - FAILED" -ForegroundColor Red
    }
    Write-Host ""
}

# Menú principal
Write-Host "Selecciona una opción:" -ForegroundColor White
Write-Host "1. Ejecutar TODAS las pruebas" -ForegroundColor White
Write-Host "2. Ejecutar solo pruebas de Controladores" -ForegroundColor White
Write-Host "3. Ejecutar solo pruebas de Integración" -ForegroundColor White
Write-Host "4. Ejecutar con cobertura de código" -ForegroundColor White
Write-Host "5. Ejecutar pruebas en watch mode (auto-reload)" -ForegroundColor White
Write-Host "6. Ver reporte detallado" -ForegroundColor White
Write-Host ""

$option = Read-Host "Opción (1-6)"

switch ($option) {
    "1" {
        Write-Host ""
        Run-TestCommand "dotnet test SistemaVoto.Tests/SistemaVoto.Tests.csproj" "Ejecutando todas las pruebas"
    }
    "2" {
        Write-Host ""
        Run-TestCommand "dotnet test SistemaVoto.Tests/SistemaVoto.Tests.csproj --filter 'FullyQualifiedName~Controllers'" "Ejecutando pruebas de Controladores"
    }
    "3" {
        Write-Host ""
        Run-TestCommand "dotnet test SistemaVoto.Tests/SistemaVoto.Tests.csproj --filter 'FullyQualifiedName~Integration'" "Ejecutando pruebas de Integración"
    }
    "4" {
        Write-Host ""
        Write-Host "?? Generando reporte de cobertura..." -ForegroundColor Cyan
        Run-TestCommand "dotnet test SistemaVoto.Tests/SistemaVoto.Tests.csproj --collect:'XPlat Code Coverage'" "Cobertura de código"
    }
    "5" {
        Write-Host ""
        Write-Host "?? Modo Watch activado (Ctrl+C para salir)" -ForegroundColor Cyan
        dotnet watch test --project SistemaVoto.Tests/SistemaVoto.Tests.csproj
    }
    "6" {
        Write-Host ""
        Run-TestCommand "dotnet test SistemaVoto.Tests/SistemaVoto.Tests.csproj --logger 'console;verbosity=detailed'" "Reporte detallado"
    }
    default {
        Write-Host "? Opción inválida" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "? Proceso completado" -ForegroundColor Green
Write-Host ""
Read-Host "Presiona Enter para salir"
