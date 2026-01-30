using Microsoft.AspNetCore.Mvc;
using SistemaVoto.Modelos; // Usamos tus modelos, ¡esto sí está bien!

namespace SistemaVoto.Vistas.Controllers
{
    public class CandidatosController : Controller
    {
        // GET: Candidatos
        public IActionResult Index()
        {
            // AQUÍ es donde luego conectaremos con tu API o Base de Datos.
            // Por ahora, simulamos los datos para que puedas DISEÑAR la página.
            var listaFalsa = new List<Candidato>
            {
                new Candidato { Id = 1, Nombre = "Juan Pérez", Partido = "Partido A" },
                new Candidato { Id = 2, Nombre = "Maria Lopez", Partido = "Partido B" }
            };

            return View(listaFalsa); // Esto busca el archivo Views/Candidatos/Index.cshtml
        }
    }
}