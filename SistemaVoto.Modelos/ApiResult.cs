namespace SistemaVoto.Modelos
{
    public class ApiResult<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }

        // Mantiene compatibilidad con tu firma actual
        public static ApiResult<T> Ok(T data) => Ok(data, null);

        public static ApiResult<T> Ok(T data, string? message)
        {
            return new ApiResult<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        public static ApiResult<T> Fail(string message)
        {
            return new ApiResult<T>
            {
                Success = false,
                Message = message,
                Data = default
            };
        }
    }
}
