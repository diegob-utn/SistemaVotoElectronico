using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace SistemaVoto.MVC.Infrastructure;

/// <summary>
/// Repositorio XML estático para Data Protection.
/// Almacena una clave fija en memoria para que las cookies de Identity
/// sean siempre descifrables, sin depender del filesystem.
/// Necesario para Render donde el filesystem es efímero.
/// </summary>
public class StaticXmlRepository : IXmlRepository
{
    // Clave fija generada para esta aplicación.
    // En producción real, esta clave debería estar en una variable de entorno.
    private static readonly string KeyXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<key id=""a0a0a0a0-b1b1-c2c2-d3d3-e4e4e4e4e4e4"" version=""1"">
  <creationDate>2025-01-01T00:00:00.0000000Z</creationDate>
  <activationDate>2025-01-01T00:00:00.0000000Z</activationDate>
  <expirationDate>2099-12-31T23:59:59.9999999Z</expirationDate>
  <descriptor deserializerType=""Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AuthenticatedEncryptorDescriptorDeserializer, Microsoft.AspNetCore.DataProtection"">
    <descriptor>
      <encryption algorithm=""AES_256_CBC"" />
      <validation algorithm=""HMACSHA256"" />
      <masterKey p4:requiresEncryption=""true"" xmlns:p4=""http://schemas.asp.net/2015/03/dataProtection"">
        <!-- Clave maestra para SistemaVotoMVC en Render -->
        <value>VG9kYXNMYXNDbGF2ZXNTb25VbmljYXNQYXJhRXN0ZUFwcDIwMjZTaXN0ZW1hVm90bw==</value>
      </masterKey>
    </descriptor>
  </descriptor>
</key>";

    private static readonly IReadOnlyCollection<XElement> _keys = new List<XElement>
    {
        XElement.Parse(KeyXml)
    };

    public IReadOnlyCollection<XElement> GetAllElements() => _keys;

    public void StoreElement(XElement element, string friendlyName)
    {
        // No-op: no necesitamos almacenar claves adicionales
    }
}
