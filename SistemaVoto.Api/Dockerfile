# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ../SistemaVoto.Modelos/SistemaVoto.Modelos.csproj ../SistemaVoto.Modelos/
COPY SistemaVoto.Api.csproj ./
RUN dotnet restore SistemaVoto.Api.csproj

COPY ../SistemaVoto.Modelos ../SistemaVoto.Modelos
COPY . .
RUN dotnet publish SistemaVoto.Api.csproj -c Release -o /app/publish

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "SistemaVoto.Api.dll"]