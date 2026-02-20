using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
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
            Crud<Ubicacion>.UrlBase = $"{apiBaseUrl}/api/ubicaciones";
            Crud<RecintoElectoral>.UrlBase = $"{apiBaseUrl}/api/recintos";
            Crud<EleccionUbicacion>.UrlBase = $"{apiBaseUrl}/api/eleccionubicaciones";

            // Configurar DbContext con PostgreSQL (misma BD que la API)
            var connectionString = builder.Configuration.GetConnectionString("DbContext.postgres-render")
                ?? builder.Configuration.GetConnectionString("DefaultConnection");
            
            // --- DEBUG CONN ---
            if (!string.IsNullOrEmpty(connectionString)) {
                var host = connectionString.Split(';').FirstOrDefault(s => s.Trim().StartsWith("Host=", StringComparison.OrdinalIgnoreCase)) ?? "Host=???";
                Console.WriteLine($"[MVC-STARTUP] CONECTANDO A BD: {host}");
            }
            // ------------------

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

            // Configurar cookies de Identity (compatible con reverse proxy HTTP)
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.AccessDeniedPath = "/Auth/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromHours(4);
                options.SlidingExpiration = true;
                // FIX RENDER: Forzar None porque ForwardedHeaders hace que el request
                // se vea como HTTPS (por X-Forwarded-Proto), lo cual marca las cookies
                // como Secure. Pero el navegador las envía por HTTPS de Render, así que
                // en realidad SÍ son seguras — solo que el servidor interno ve HTTP.
                options.Cookie.SecurePolicy = CookieSecurePolicy.None;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            // Configurar anti-forgery token (compatible con reverse proxy HTTP)
            builder.Services.AddAntiforgery(options =>
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.None;
                options.Cookie.SameSite = SameSiteMode.Lax;
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
            builder.Services.AddScoped<LocalCrudService>();
            
            // Servicios en segundo plano (Fase 11)
            builder.Services.AddHostedService<ElectionBackgroundService>();
            builder.Services.AddScoped<ElectionManagerService>();

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            // --- FIX RENDER: Data Protection para contenedores Docker ---
            // Sin esto, cada reinicio del contenedor genera claves nuevas
            // y los anti-forgery tokens se invalidan
            builder.Services.AddDataProtection()
                .SetApplicationName("SistemaVotoMVC")
                .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"));

            // --- FIX RENDER: Configurar ForwardedHeaders como servicio ---
            // Render usa un Load Balancer que termina SSL. La app recibe HTTP internamente.
            // Necesitamos confiar en los headers X-Forwarded-* del proxy de Render.
            builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                                           Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
                // Limpiar restricciones para confiar en el proxy de Render (no es localhost)
                options.KnownProxies.Clear();
                options.KnownNetworks.Clear();
            });

            var app = builder.Build();

            // PRIMERO: ForwardedHeaders debe ir antes de cualquier otro middleware
            app.UseForwardedHeaders();

            // --- DIAGNÓSTICO: Log del esquema detectado ---
            app.Use(async (context, next) =>
            {
                var scheme = context.Request.Scheme;
                var host = context.Request.Host;
                var forwardedProto = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
                Console.WriteLine($"[MVC-REQUEST] Scheme={scheme}, Host={host}, X-Forwarded-Proto={forwardedProto}, Path={context.Request.Path}");
                await next();
            });

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // No usar HSTS ni HTTPS Redirection en Render
                // Render maneja SSL en su load balancer, la app recibe HTTP internamente
            }
            else
            {
                // Solo redirigir a HTTPS en desarrollo local
                app.UseHttpsRedirection();
            }
            app.UseStaticFiles();

            app.UseRouting();

            // Middlewares de autenticacion y autorizacion
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            // Seed de roles al iniciar (con try-catch para no crashear si la BD falla)
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                    
                    SeedRolesAndAdminAsync(roleManager, userManager).GetAwaiter().GetResult();
                    Console.WriteLine("[MVC-STARTUP] Seed de roles completado exitosamente.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MVC-STARTUP] ERROR en seed de roles: {ex.Message}");
                // No crashear la app, seguir adelante
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
