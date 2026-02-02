using Microsoft.AspNetCore.Authentication.Cookies; // Necesario para cookies

namespace SistemaVoto.MVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // -----------------------------------------------------------
            // 1. ELIMINAR O COMENTAR TODO ESTO (LO QUE BORRAS):
            // -----------------------------------------------------------
            /*
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<IdentityUser>(...)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            */

            // -----------------------------------------------------------
            // 2. AGREGAR ESTO (LA CONFIGURACIÓN CLIENTE API):
            // -----------------------------------------------------------

            // Configurar HttpClient para hablar con la API
            var apiUrl = builder.Configuration.GetValue<string>("ApiSettings:BaseUrl");
            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri(apiUrl!)
            });

            // Configurar Autenticación por Cookies (El MVC recuerda al usuario con una cookie)
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Cuenta/Login"; // Ruta a tu vista de login
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                });

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // ... (Resto de la configuración de entorno igual) ...

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // -----------------------------------------------------------
            // 3. ACTIVAR LOS MIDDLEWARES DE SEGURIDAD
            // -----------------------------------------------------------
            app.UseAuthentication(); // <--- IMPORTANTE: Identifica quién es el usuario (lee la cookie)
            app.UseAuthorization();  // <--- IMPORTANTE: Verifica qué permisos tiene

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // app.MapRazorPages(); // <--- ELIMINAR ESTO (Ya no usas las páginas automáticas de Identity)

            app.Run();
        }
    }
}