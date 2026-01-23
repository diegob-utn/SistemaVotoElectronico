FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["SistemaVoto.Api/SistemaVoto.Api.csproj", "SistemaVoto.Api/"]
#COPY ["../SistemaVoto.Modelos/SistemaVoto.Modelos.csproj", "SistemaVoto.Modelos/"]
RUN dotnet restore "SistemaVoto.Api/SistemaVoto.Api.csproj"

COPY . .
WORKDIR "/src/SistemaVoto.Api"
RUN dotnet build "SistemaVoto.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SistemaVoto.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SistemaVoto.Api.dll"]
