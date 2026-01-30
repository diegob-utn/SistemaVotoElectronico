using Microsoft.AspNetCore.Mvc;
using SistemaVoto.Modelos;

namespace SistemaVoto.Vistas.Controllers
{
    public class VotacionController : Controller
    {
        public IActionResult Urna()
        {
            var candidatos = new List<Candidato>
            {
                new Candidato { Id = 1, Nombre = "Candidato A", Partido = "Lista 1" },
                new Candidato { Id = 2, Nombre = "Candidato B", Partido = "Lista 2" }
            };
            return View(candidatos);
        }
    }
}