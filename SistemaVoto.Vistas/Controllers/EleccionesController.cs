using Microsoft.AspNetCore.Mvc;
using SistemaVoto.Modelos;

namespace SistemaVoto.Vistas.Controllers
{
    public class EleccionesController : Controller
    {
        public IActionResult Index()
        {
            var listaFalsa = new List<Eleccion>
            {
                new Eleccion { Id = 1, Descripcion = "Elecciones 2026", Activo = true }
            };
            return View(listaFalsa);
        }
    }
}