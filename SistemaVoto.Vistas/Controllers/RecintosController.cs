using Microsoft.AspNetCore.Mvc;
using SistemaVoto.Modelos; // Asegúrate de tener la clase RecintoElectoral o similar en tus modelos

namespace SistemaVoto.Vistas.Controllers
{
    public class RecintosController : Controller
    {
        public IActionResult Index()
        {
            // Datos de prueba
            // NOTA: Si tu modelo se llama diferente (ej: RecintoElectoral), cámbialo aquí
            var lista = new List<RecintoElectoral>
            {
                new RecintoElectoral { Id = 1, Nombre = "Colegio Central", Direccion = "Av. Principal 123" },
                new RecintoElectoral { Id = 2, Nombre = "Escuela del Sur", Direccion = "Calle 5ta y 8va" }
            };
            return View(lista);
        }
    }
}