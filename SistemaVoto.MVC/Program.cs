using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaVoto.Data.Data;
using SistemaVoto.MVC.Services;
using SistemaVoto.ApiConsumer;
using SistemaVoto.Modelos;

namespace SistemaVoto.MVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuracion de la API
            var apiBaseUrl = builder.Configuration["Api:BaseUrl"] 
                ?? "https://sistemavotoelectronico.onrender.com";

            // Configurar URLs de la API para Crud<T>
            Crud<Eleccion>.UrlBase = $"{apiBaseUrl}/api/elecciones";
            Crud<Candidato>.UrlBase = $"{apiBaseUrl}/api/candidatos";
            Crud<Voto>.UrlBase = $"{apiBaseUrl}/api/votos";
            Crud<Lista>.UrlBase = $"{apiBaseUrl}/api/listas";

            // Configurar DbContext con PostgreSQL (misma BD que la API)
            var connectionString = builder.Configuration.GetConnectionString("DbContext.postgres-render")
                ?? builder.Configuration.GetConnectionString("DefaultConnection");
            
            builder.Services.AddDbContext<SistemaVotoDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Configurar ASP.NET Identity con las tablas AspNet*
            builder.Services.AddDefaultIdentity<IdentityUser>(options => 
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 4;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<SistemaVotoDbContext>();

            // Configurar cookies de Identity
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.AccessDeniedPath = "/Auth/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromHours(4);
                options.SlidingExpiration = true;
            });

            // Registrar HttpContextAccessor
            builder.Services.AddHttpContextAccessor();

            // Registrar HttpClient para ApiService
            builder.Services.AddHttpClient<ApiService>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            });

            // Registrar servicios
            builder.Services.AddScoped<JwtAuthService>();
            builder.Services.AddScoped<CalculoEscanosService>();

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Middlewares de autenticacion y autorizacion
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            // Seed de roles al iniciar
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                
                SeedRolesAndAdminAsync(roleManager, userManager).GetAwaiter().GetResult();
            }

            app.Run();
        }

        /// <summary>
        /// Crea los roles Administrador y Usuario si no existen
        /// </summary>
        private static async Task SeedRolesAndAdminAsync(
            RoleManager<IdentityRole> roleManager, 
            UserManager<IdentityUser> userManager)
        {
            // Crear roles
            string[] roles = { "Administrador", "Usuario" };
            
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    Console.WriteLine($"Rol '{role}' creado exitosamente.");
                }
            }

            // Crear usuario admin por defecto si no existe
            var adminEmail = "admin@sistemavoto.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrador");
                    Console.WriteLine($"Usuario admin creado: {adminEmail} / Admin123!");
                }
            }
        }
    }
}
