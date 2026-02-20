using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SistemaVoto.MVC.Services;

/// <summary>
/// Servicio de autenticación JWT
/// </summary>
public class JwtAuthService
{
    private readonly ApiService _api;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string JWT_COOKIE_NAME = "jwt_token";
    private const string USER_COOKIE_NAME = "user_info";

    public JwtAuthService(ApiService apiService, IHttpContextAccessor httpContextAccessor)
    {
        _api = apiService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Intenta autenticar al usuario con la API
    /// </summary>
    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var result = await _api.PostAsync<LoginResponse>("api/auth/login", new { email, password });
        
        if (!result.Success || result.Data == null)
        {
            return new AuthResult
            {
                Success = false,
                Message = result.Message ?? "Error de autenticación"
            };
        }

        // Guardar token en cookie HttpOnly
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(4)
        };
        
        _httpContextAccessor.HttpContext?.Response.Cookies.Append(
            JWT_COOKIE_NAME, 
            result.Data.Token, 
            cookieOptions);

        // Guardar info de usuario (no sensible) en cookie accesible por JS
        var userCookieOptions = new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(4)
        };
        
        var userJson = System.Text.Json.JsonSerializer.Serialize(result.Data.User);
        _httpContextAccessor.HttpContext?.Response.Cookies.Append(
            USER_COOKIE_NAME, 
            userJson, 
            userCookieOptions);

        return new AuthResult
        {
            Success = true,
            User = result.Data.User,
            Token = result.Data.Token
        };
    }

    /// <summary>
    /// Cierra la sesión eliminando las cookies
    /// </summary>
    public void Logout()
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete(JWT_COOKIE_NAME);
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete(USER_COOKIE_NAME);
    }

    /// <summary>
    /// Obtiene los claims del token JWT actual
    /// </summary>
    public ClaimsPrincipal? GetCurrentUser()
    {
        var token = _httpContextAccessor.HttpContext?.Request.Cookies[JWT_COOKIE_NAME];
        
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            var claims = jwtToken.Claims.ToList();
            var identity = new ClaimsIdentity(claims, "jwt");
            
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Verifica si el usuario está autenticado
    /// </summary>
    public bool IsAuthenticated()
    {
        var token = _httpContextAccessor.HttpContext?.Request.Cookies[JWT_COOKIE_NAME];
        
        if (string.IsNullOrEmpty(token))
            return false;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            return jwtToken.ValidTo > DateTime.UtcNow;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Obtiene el rol del usuario actual
    /// </summary>
    public string? GetCurrentUserRole()
    {
        var user = GetCurrentUser();
        return user?.FindFirst(ClaimTypes.Role)?.Value;
    }
}

/// <summary>
/// Resultado de autenticación
/// </summary>
public class AuthResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; }
    public UserInfo? User { get; set; }
}

/// <summary>
/// Respuesta del endpoint de login
/// </summary>
public class LoginResponse
{
    public string Token { get; set; } = null!;
    public UserInfo User { get; set; } = null!;
}

/// <summary>
/// Información básica del usuario
/// </summary>
public class UserInfo
{
    public string Id { get; set; } = null!;
    public string NombreCompleto { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Role { get; set; }
}
