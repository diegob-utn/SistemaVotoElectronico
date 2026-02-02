using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaVoto.MVC.ViewModels;

namespace SistemaVoto.MVC.Controllers;

/// <summary>
/// Controlador de autenticacion usando ASP.NET Identity
/// </summary>
public class AuthController : Controller
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public AuthController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Muestra el formulario de login
    /// </summary>
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Si ya esta autenticado, redirigir
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToHome();
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    /// <summary>
    /// Procesa el login con Identity
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Intentar login con Identity (Email como username)
        var result = await _signInManager.PasswordSignInAsync(
            model.Email, 
            model.Password, 
            model.RememberMe, 
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            // Login exitoso
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Administrador"))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
            }

            return RedirectToAction("Index", "Elecciones");
        }

        if (result.IsLockedOut)
        {
            model.ErrorMessage = "Cuenta bloqueada. Intente mas tarde.";
        }
        else if (result.IsNotAllowed)
        {
            model.ErrorMessage = "Login no permitido. Verifique su cuenta.";
        }
        else
        {
            model.ErrorMessage = "Credenciales invalidas";
        }

        return View(model);
    }

    /// <summary>
    /// Cierra la sesion
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }

    /// <summary>
    /// Redirige al home segun el rol del usuario
    /// </summary>
    private IActionResult RedirectToHome()
    {
        if (User.IsInRole("Administrador"))
        {
            return RedirectToAction("Dashboard", "Admin");
        }

        return RedirectToAction("Index", "Elecciones");
    }

    /// <summary>
    /// Acceso denegado
    /// </summary>
    public IActionResult AccessDenied()
    {
        return View();
    }
}
