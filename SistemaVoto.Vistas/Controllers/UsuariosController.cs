using Microsoft.AspNetCore.Mvc;
using SistemaVoto.Modelos;

namespace SistemaVoto.Vistas.Controllers
{
    public class UsuariosController : Controller
    {
        public IActionResult Index()
        {
            // Datos de prueba
            var lista = new List<Usuario>
            {
                new Usuario { Id = 1, NombreUsuario = "admin", NombreCompleto = "Administrador Principal", Activo = true },
                new Usuario { Id = 2, NombreUsuario = "operador1", NombreCompleto = "Juan Operador", Activo = true },
                new Usuario { Id = 3, NombreUsuario = "invitado", NombreCompleto = "Visitante", Activo = false }
            };
            return View(lista);
        }
    }
}