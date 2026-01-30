using Microsoft.EntityFrameworkCore;
using SistemaVoto.Api.Data;

namespace SistemaVoto.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddDbContext<SistemaVotoDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DbContext.postgres-render")
                ?? throw new InvalidOperationException("Connection string 'DbContext.postgres-render' not found."))); // Corregido el mensaje de error para coincidir con la clave

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSignalR();
            builder.Services.AddScoped<SistemaVoto.Api.Services.VoteHashService>();
            builder.Services.AddScoped<SistemaVoto.Api.Services.SeatAllocationService>();

            builder.Services.AddCors(o =>
            {
                o.AddPolicy("AllowDashboard", p =>
                    p.WithOrigins(
                        "http://localhost:5500",
                        "http://127.0.0.1:5500"
                    // aquí luego pones el dominio donde hostees el html (vercel/netlify/etc)
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
            });


            var app = builder.Build();
            // ...

            app.MapHub<SistemaVoto.Api.Hubs.VotacionHub>("/hubs/votacion");

            app.UseCors("AllowDashboard");

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment()) // Comentado para ver Swagger en Producción/Render si lo deseas
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            // =================================================================
            //  SOLUCIÓN: APLICAR MIGRACIONES AUTOMÁTICAMENTE AL INICIAR
            // =================================================================
            // Esto crea las tablas en Render si no existen.
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var dbContext = services.GetRequiredService<SistemaVotoDbContext>();

                    Console.WriteLine(" Intentando aplicar migraciones...");
                    dbContext.Database.Migrate();
                    Console.WriteLine(" Base de datos migrada exitosamente (Tablas creadas).");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" Error crítico al migrar la base de datos: {ex.Message}");
                    // Opcional: throw; si quieres que la app se detenga si falla la DB
                }
            }
            // =================================================================

            app.Run();
        }
    }
}