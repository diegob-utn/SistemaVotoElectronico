using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SistemaVoto.MVC.Services;

/// <summary>
/// Servicio base para comunicación con la API REST
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration config)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        
        // Configurar base URL desde appsettings
        var baseUrl = config["Api:BaseUrl"] ?? "https://sistemavotoelectronico.onrender.com";
        _httpClient.BaseAddress = new Uri(baseUrl);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Obtiene el token JWT almacenado en las cookies
    /// </summary>
    private string? GetToken()
    {
        return _httpContextAccessor.HttpContext?.Request.Cookies["jwt_token"];
    }

    /// <summary>
    /// Agrega el header de autorización si hay token disponible
    /// </summary>
    private void AddAuthHeader()
    {
        var token = GetToken();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    /// <summary>
    /// GET request a la API
    /// </summary>
    public async Task<ApiResult<T>> GetAsync<T>(string endpoint)
    {
        try
        {
            AddAuthHeader();
            var response = await _httpClient.GetAsync(endpoint);
            return await ParseResponse<T>(response);
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Fail($"Error de conexión: {ex.Message}");
        }
    }

    /// <summary>
    /// POST request a la API
    /// </summary>
    public async Task<ApiResult<T>> PostAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            AddAuthHeader();
            var content = data != null 
                ? new StringContent(JsonSerializer.Serialize(data, _jsonOptions), Encoding.UTF8, "application/json")
                : null;
            
            var response = await _httpClient.PostAsync(endpoint, content);
            return await ParseResponse<T>(response);
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Fail($"Error de conexión: {ex.Message}");
        }
    }

    /// <summary>
    /// PUT request a la API
    /// </summary>
    public async Task<ApiResult<T>> PutAsync<T>(string endpoint, object data)
    {
        try
        {
            AddAuthHeader();
            var content = new StringContent(
                JsonSerializer.Serialize(data, _jsonOptions), 
                Encoding.UTF8, 
                "application/json");
            
            var response = await _httpClient.PutAsync(endpoint, content);
            return await ParseResponse<T>(response);
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Fail($"Error de conexión: {ex.Message}");
        }
    }

    /// <summary>
    /// DELETE request a la API
    /// </summary>
    public async Task<ApiResult<T>> DeleteAsync<T>(string endpoint)
    {
        try
        {
            AddAuthHeader();
            var response = await _httpClient.DeleteAsync(endpoint);
            return await ParseResponse<T>(response);
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Fail($"Error de conexión: {ex.Message}");
        }
    }

    /// <summary>
    /// Parsea la respuesta de la API al formato ApiResult
    /// </summary>
    private async Task<ApiResult<T>> ParseResponse<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        
        if (string.IsNullOrEmpty(json))
        {
            return response.IsSuccessStatusCode 
                ? ApiResult<T>.Ok(default!)
                : ApiResult<T>.Fail("Respuesta vacía del servidor");
        }
        
        try
        {
            var result = JsonSerializer.Deserialize<ApiResult<T>>(json, _jsonOptions);
            return result ?? ApiResult<T>.Fail("Error al deserializar respuesta");
        }
        catch
        {
            // Si falla el parseo, intentar retornar el mensaje de error
            return ApiResult<T>.Fail($"Error del servidor: {response.StatusCode}");
        }
    }
}

/// <summary>
/// Modelo genérico para respuestas de la API
/// </summary>
public class ApiResult<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static ApiResult<T> Ok(T data, string? message = null)
    {
        return new ApiResult<T> { Success = true, Data = data, Message = message };
    }

    public static ApiResult<T> Fail(string message)
    {
        return new ApiResult<T> { Success = false, Message = message };
    }
}
