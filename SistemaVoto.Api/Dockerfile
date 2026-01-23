# Etapa base: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Etapa build: SDK y restauración de dependencias
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copia solo los archivos de proyecto para aprovechar el cache de Docker
COPY ["SistemaVoto.Api/SistemaVoto.Api.csproj", "SistemaVoto.Api/"]
COPY ["SistemaVoto.Modelos/SistemaVoto.Modelos.csproj", "SistemaVoto.Modelos/"]
RUN dotnet restore "SistemaVoto.Api/SistemaVoto.Api.csproj"

# Copia el resto del código fuente
COPY . .
WORKDIR "/src/SistemaVoto.Api"
RUN dotnet build "SistemaVoto.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Etapa publish: publica la app para producción
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SistemaVoto.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Etapa final: imagen mínima solo con runtime y app publicada
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "SistemaVoto.Api.dll"]