# Dockerfile para SistemaVoto.MVC - Deploy independiente en Render
# Multi-stage build

# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solo los .csproj necesarios para el MVC
COPY SistemaVoto.Modelos/SistemaVoto.Modelos.csproj SistemaVoto.Modelos/
COPY SistemaVoto.Data/SistemaVoto.Data.csproj SistemaVoto.Data/
COPY SistemaVoto.ApiConsumer/SistemaVoto.ApiConsumer.csproj SistemaVoto.ApiConsumer/
COPY SistemaVoto.MVC/SistemaVoto.MVC.csproj SistemaVoto.MVC/

# Restaurar dependencias
RUN dotnet restore SistemaVoto.MVC/SistemaVoto.MVC.csproj

# Copiar código fuente necesario
COPY SistemaVoto.Modelos/ SistemaVoto.Modelos/
COPY SistemaVoto.Data/ SistemaVoto.Data/
COPY SistemaVoto.ApiConsumer/ SistemaVoto.ApiConsumer/
COPY SistemaVoto.MVC/ SistemaVoto.MVC/

# Publicar
WORKDIR /src/SistemaVoto.MVC
RUN dotnet publish SistemaVoto.MVC.csproj -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Crear directorio para Data Protection keys (anti-forgery tokens)
RUN mkdir -p /app/keys

# Render usa puerto 10000 por defecto
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000

ENTRYPOINT ["dotnet", "SistemaVoto.MVC.dll"]