# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY nuget.config ./

# 1. COPIAR TODOS LOS .CSPROJ
# Es vital copiar el de Data aquí, porque la API lo necesita para restaurar
COPY SistemaVoto.Modelos/SistemaVoto.Modelos.csproj SistemaVoto.Modelos/
COPY SistemaVoto.Data/SistemaVoto.Data.csproj SistemaVoto.Data/
COPY SistemaVoto.Api/SistemaVoto.Api.csproj SistemaVoto.Api/

# 2. RESTAURAR
# Al restaurar la Api, buscará los otros proyectos. Ahora sí los encontrará.
RUN dotnet restore SistemaVoto.Api/SistemaVoto.Api.csproj

# 3. COPIAR TODO EL CÓDIGO FUENTE
# Aquí copiamos los archivos .cs reales de todas las capas
COPY SistemaVoto.Modelos/ SistemaVoto.Modelos/
COPY SistemaVoto.Data/ SistemaVoto.Data/
COPY SistemaVoto.Api/ SistemaVoto.Api/

# 4. PUBLICAR
WORKDIR /src/SistemaVoto.Api
RUN dotnet publish SistemaVoto.Api.csproj -c Release -o /app/publish

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render suele usar el puerto 10000 o dinámicos, pero internamente 
# en el contenedor usaremos el 8080 para evitar problemas de permisos.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SistemaVoto.Api.dll"]