# Dockerfile Unificado para SistemaVoto (API y MVC)
# Este archivo permite desplegar cualquiera de los dos proyectos.

# 1. IMAGEN BASE DE SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 2. COPIAR TODOS LOS PROYECTOS (Restauración de Dependencias)
COPY SistemaVoto.Modelos/SistemaVoto.Modelos.csproj SistemaVoto.Modelos/
COPY SistemaVoto.Data/SistemaVoto.Data.csproj SistemaVoto.Data/
COPY SistemaVoto.ApiConsumer/SistemaVoto.ApiConsumer.csproj SistemaVoto.ApiConsumer/
COPY SistemaVoto.Api/SistemaVoto.Api.csproj SistemaVoto.Api/
COPY SistemaVoto.MVC/SistemaVoto.MVC.csproj SistemaVoto.MVC/

# 3. RESTAURAR
# Al restaurar MVC, también restaura sus dependencias (Modelos, ApiConsumer, etc.)
RUN dotnet restore SistemaVoto.MVC/SistemaVoto.MVC.csproj
RUN dotnet restore SistemaVoto.Api/SistemaVoto.Api.csproj

# 4. COPIAR CÓDIGO FUENTE COMPLETO
COPY SistemaVoto.Modelos/ SistemaVoto.Modelos/
COPY SistemaVoto.Data/ SistemaVoto.Data/
COPY SistemaVoto.ApiConsumer/ SistemaVoto.ApiConsumer/
COPY SistemaVoto.Api/ SistemaVoto.Api/
COPY SistemaVoto.MVC/ SistemaVoto.MVC/

# 5. PUBLICAR (Generamos ambas carpetas de salida)
WORKDIR /src/SistemaVoto.Api
RUN dotnet publish SistemaVoto.Api.csproj -c Release -o /app/publish/api

WORKDIR /src/SistemaVoto.MVC
RUN dotnet publish SistemaVoto.MVC.csproj -c Release -o /app/publish/mvc /p:UseAppHost=false

# 6. IMAGEN FINAL (RUNTIME)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Puerto por defecto de Render
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# POR DEFECTO: Ejecuta MVC (que es lo que el usuario quiere ahora)
# Si se quisiera ejecutar la API, se cambiaría el ENTRYPOINT en Render
COPY --from=build /app/publish/mvc .
# Copiamos también la API por si acaso se quisiera usar la misma imagen
# COPY --from=build /app/publish/api ./api_bin 

ENTRYPOINT ["dotnet", "SistemaVoto.MVC.dll"]