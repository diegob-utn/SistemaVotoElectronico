using Microsoft.EntityFrameworkCore;
using SistemaVoto.Api.Data;
using SistemaVoto.Api.Hubs;
using SistemaVoto.Api.Services;

namespace SistemaVoto.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // -----------------------------
            // DB (PostgreSQL - Npgsql)
            // -----------------------------
            builder.Services.AddDbContext<SistemaVotoDbContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DbContext.postgres-render")
                    ?? throw new InvalidOperationException("Connection string 'DbContext.postgresql' not found.")
                )
            );

            // -----------------------------
            // Controllers / Swagger
            // -----------------------------
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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
                    // Cuando hostees el dashboard, agrega aquí:
                    // "https://tu-dashboard.vercel.app"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                );
            });

            var app = builder.Build();

            // -----------------------------
            // Health endpoint (Render)
            // -----------------------------
            app.MapGet("/health", () => Results.Ok(new { ok = true, utc = DateTime.UtcNow }));

            // -----------------------------
            // Migraciones automáticas (Render)
            // -----------------------------
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
                    // Si quieres que NO arranque si falla la migración, descomenta:
                    // throw;
                }
            }

            // -----------------------------
            // Middleware order (importante)
            // -----------------------------
            //if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors("AllowDashboard");

            app.UseAuthorization();

            // -----------------------------
            // Endpoints
            // -----------------------------
            app.MapControllers();
            app.MapHub<VotacionHub>("/hubs/votacion");

            app.Run();
        }
    }
}
