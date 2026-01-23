


# Build stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SistemaVoto.Api/SistemaVoto.Api.csproj", "SistemaVoto.Api/"]
COPY ["SistemaVoto.Modelos/SistemaVoto.Modelos.csproj", "SistemaVoto.Modelos/"]
RUN dotnet restore "SistemaVoto.Api/SistemaVoto.Api.csproj"
COPY . .
WORKDIR "/src/SistemaVoto.Api"
RUN dotnet build "SistemaVoto.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SistemaVoto.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SistemaVoto.Api.dll"]