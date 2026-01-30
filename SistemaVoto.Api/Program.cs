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
    ?? throw new InvalidOperationException("Connection string 'DbContext.postgresql' not found.")));

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
            //if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
