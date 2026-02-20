using SistemaVoto.Modelos;

namespace SistemaVoto.ApiConsumer
{
    public static class Crud<T>
    {
        public static string UrlBase = "";

        /// <summary>
        /// Consumir API y ejecutar POST (Crear)
        /// </summary>
        public static ApiResult<T> Create(T data)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = httpClient.PostAsync(UrlBase, content).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        json = response.Content.ReadAsStringAsync().Result;
                        if (string.IsNullOrEmpty(json)) return ApiResult<T>.Fail("Respuesta vacía de API");

                        var newData = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResult<T>>(json);
                        return newData ?? ApiResult<T>.Fail("Error deserializando respuesta");
                    }
                    else
                    {
                        return ApiResult<T>.Fail($"Error: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                return ApiResult<T>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Consumir API y ejecutar GET (Leer todos)
        /// </summary>
        public static ApiResult<List<T>> ReadAll()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = httpClient.GetAsync(UrlBase).Result;
                    var json = response.Content.ReadAsStringAsync().Result;

                    // Parsear como JObject para inspeccionar estructura
                    var root = Newtonsoft.Json.Linq.JObject.Parse(json);
                    if (root == null) return ApiResult<List<T>>.Fail("JSON inválido");

                    bool success = (bool?)root["success"] ?? false;
                    
                    if (!success)
                    {
                         string msg = (string?)root["message"] ?? "Error desconocido en API";
                         return ApiResult<List<T>>.Fail(msg);
                    }

                    var dataToken = root["data"];
                    if (dataToken == null || dataToken.Type == Newtonsoft.Json.Linq.JTokenType.Null)
                    {
                        return ApiResult<List<T>>.Ok(new List<T>());
                    }

                    List<T> list = new List<T>();

                    // Caso 1: Array directo (Lista simple)
                    if (dataToken is Newtonsoft.Json.Linq.JArray)
                    {
                        var result = dataToken.ToObject<List<T>>();
                        if (result != null) list = result;
                    }
                    // Caso 2: Objeto Paginado (tiene propiedad "items")
                    else if (dataToken is Newtonsoft.Json.Linq.JObject && dataToken["items"] is Newtonsoft.Json.Linq.JArray itemsArray)
                    {
                        var result = itemsArray.ToObject<List<T>>();
                        if (result != null) list = result;
                    }
                    else
                    {
                        // Intentar deserializar como lista de todas formas por si acaso
                         try {
                              var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<T>>(dataToken.ToString());
                              if (result != null) list = result;
                         } catch {
                              return ApiResult<List<T>>.Fail("Formato de respuesta no reconocido (Ni lista ni paginado)");
                         }
                    }

                    return ApiResult<List<T>>.Ok(list);
                }
            }
            catch (Exception ex)
            {
                return ApiResult<List<T>>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Consumir API y ejecutar GET por campo/valor
        /// </summary>
        public static ApiResult<T> ReadBy(string field, string value)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = httpClient.GetAsync($"{UrlBase}/{field}/{value}").Result;
                    var json = response.Content.ReadAsStringAsync().Result;
                    if (string.IsNullOrEmpty(json)) return ApiResult<T>.Fail("Respuesta vacía");

                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResult<T>>(json);
                    return data ?? ApiResult<T>.Fail("Error deserializando respuesta");
                }
            }
            catch (Exception ex)
            {
                return ApiResult<T>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Consumir API y ejecutar GET por ID
        /// </summary>
        public static ApiResult<T> ReadById(object id)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = httpClient.GetAsync($"{UrlBase}/{id}").Result;
                    var json = response.Content.ReadAsStringAsync().Result;
                    if (string.IsNullOrEmpty(json)) return ApiResult<T>.Fail("Respuesta vacía");

                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResult<T>>(json);
                    return data ?? ApiResult<T>.Fail("Error deserializando respuesta");
                }
            }
            catch (Exception ex)
            {
                return ApiResult<T>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Consumir API y ejecutar PUT (Actualizar)
        /// </summary>
        public static ApiResult<bool> Update(string id, T data)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = httpClient.PutAsync($"{UrlBase}/{id}", content).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        return ApiResult<bool>.Ok(true);
                    }
                    else
                    {
                        return ApiResult<bool>.Fail($"Error: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Consumir API y ejecutar PUT con objeto generico
        /// </summary>
        public static ApiResult<bool> Update(object id, object data)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = httpClient.PutAsync($"{UrlBase}/{id}", content).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        return ApiResult<bool>.Ok(true);
                    }
                    else
                    {
                        return ApiResult<bool>.Fail($"Error: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Consumir API y ejecutar DELETE (Eliminar)
        /// </summary>
        public static ApiResult<bool> Delete(string id)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = httpClient.DeleteAsync($"{UrlBase}/{id}").Result;
                    if (response.IsSuccessStatusCode)
                    {
                        return ApiResult<bool>.Ok(true);
                    }
                    else
                    {
                        return ApiResult<bool>.Fail($"Error: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Consumir API y ejecutar DELETE con id numerico
        /// </summary>
        public static ApiResult<bool> Delete(int id)
        {
            return Delete(id.ToString());
        }
    }
}
