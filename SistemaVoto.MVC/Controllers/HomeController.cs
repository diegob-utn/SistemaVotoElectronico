using Microsoft.AspNetCore.Mvc;
using SistemaVoto.MVC.Models;
using System.Diagnostics;

namespace SistemaVoto.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.IsInRole("Administrador"))
                return RedirectToAction("Dashboard", "Admin");

            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Elecciones");

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
