# Utiliza la imagen oficial de .NET 8 para construir la aplicación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia los archivos del proyecto y restaura las dependencias
COPY ["SistemaVoto.Api/SistemaVoto.Api.csproj", "SistemaVoto.Api/"]
COPY ["SistemaVoto.Modelos/SistemaVoto.Modelos.csproj", "SistemaVoto.Modelos/"]
RUN dotnet restore "SistemaVoto.Api/SistemaVoto.Api.csproj"

# Copia el resto de los archivos y compila la aplicación
COPY . .
WORKDIR "/src/SistemaVoto.Api"
RUN dotnet publish "SistemaVoto.Api.csproj" -c Release -o /app/publish

# Utiliza la imagen de runtime para ejecutar la aplicación
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "SistemaVoto.Api.dll"]