using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SistemaVoto.Modelos; // <--- ¡MUY IMPORTANTE!

namespace SistemaVoto.Vistas.Controllers
{
    public class RolesController : Controller
    {
        // GET: RolesController
        public ActionResult Index()
        {
            // DATOS DE PRUEBA (MOCK DATA)
            // Esto simula que los trajimos de la Base de Datos
            var listaRoles = new List<Rol>
            {
                new Rol { Id = 1, Descripcion = "Administrador", Activo = true },
                new Rol { Id = 2, Descripcion = "Digitador", Activo = true },
                new Rol { Id = 3, Descripcion = "Veedor", Activo = false },
                new Rol { Id = 4, Descripcion = "Invitado", Activo = true }
            };

            return View(listaRoles); // Enviamos la lista a la vista
        }

        // ... (Puedes dejar el resto de métodos Create, Edit, Delete tal cual están por ahora)
        public ActionResult Details(int id) { return View(); }
        public ActionResult Create() { return View(); }
        // ...
    }
}