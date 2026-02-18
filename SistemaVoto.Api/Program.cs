using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SistemaVoto.Data.Data;

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
                ?? throw new InvalidOperationException("Connection string 'DbContext.postgres-render' not found.")));

            // Configurar Identity (Necesario para UserManager)
            builder.Services.AddIdentityCore<IdentityUser>(options => 
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 4;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<SistemaVotoDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.AddControllers();

            // JWT Configuration
            var keyStr = builder.Configuration["Jwt:Key"] ?? "SecretKey_SuperSegura_Para_Desarrollo_123456";
            var key = System.Text.Encoding.UTF8.GetBytes(keyStr);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSignalR();
            builder.Services.AddScoped<SistemaVoto.Api.Services.VoteHashService>();
            builder.Services.AddScoped<SistemaVoto.Api.Services.SeatAllocationService>();

            builder.Services.AddCors(o =>
            {
                o.AddPolicy("AllowDashboard", p =>
                    p.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
            });


            var app = builder.Build();

            app.MapHub<SistemaVoto.Api.Hubs.VotacionHub>("/hubs/votacion");

            app.UseCors("AllowDashboard");

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment()) 
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication(); // JWT Middleare
            app.UseAuthorization();

            app.MapControllers();

            // =================================================================
            //  SOLUCIÓN: APLICAR MIGRACIONES AUTOMÁTICAMENTE AL INICIAR
            // =================================================================
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
                }
            }
            // =================================================================

            app.Run();
        }
    }
}
