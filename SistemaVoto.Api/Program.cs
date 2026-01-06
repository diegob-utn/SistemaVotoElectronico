using Microsoft.EntityFrameworkCore;
using SistemaVoto.Api.Data;
using SistemaVoto.Api.Hubs;
using SistemaVoto.Api.Services;
using Microsoft.OpenApi.Models;

namespace SistemaVoto.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // -----------------------------
            // DB (PostgreSQL - Render)
            // -----------------------------
            builder.Services.AddDbContext<SistemaVotoDbContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DbContext.postgres-render")
                    ?? throw new InvalidOperationException("Connection string 'DbContext.postgres-render' not found.")
                )
            );

            // -----------------------------
            // Controllers / Swagger
            // -----------------------------
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "SistemaVoto API",
                    Version = "v1"
                });

                // Evita choques de modelos con mismo nombre
                c.CustomSchemaIds(t => t.FullName);

                // Evita que Swagger reviente si hay rutas duplicadas (parche)
                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
            });

            // -----------------------------
            // SignalR + Services
            // -----------------------------
            builder.Services.AddSignalR();
            builder.Services.AddScoped<VoteHashService>();
            builder.Services.AddScoped<SeatAllocationService>();

            // -----------------------------
            // CORS (Dashboard + SignalR)
            // -----------------------------
            builder.Services.AddCors(o =>
            {
                o.AddPolicy("AllowDashboard", p =>
                    p.WithOrigins(
                        "http://localhost:5500",
                        "http://127.0.0.1:5500"
                    // agrega aquí tu dominio del dashboard cuando lo subas
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                );
            });

            var app = builder.Build();

            // Health
            app.MapGet("/health", () => Results.Ok(new { ok = true, utc = DateTime.UtcNow }));

            // Migraciones automáticas
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    var db = services.GetRequiredService<SistemaVotoDbContext>();
                    db.Database.Migrate();
                    logger.LogInformation(" Migraciones aplicadas correctamente.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, " Error aplicando migraciones automáticas.");
                    // throw; // si quieres que no arranque si falla
                }
            }

            // Swagger (si quieres que se vea en Render, pon ASPNETCORE_ENVIRONMENT=Development)
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SistemaVoto API v1");
                });
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowDashboard");
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<VotacionHub>("/hubs/votacion");

            app.Run();
        }
    }
}
