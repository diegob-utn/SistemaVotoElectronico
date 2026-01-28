using System.Security.Cryptography;
using System.Text;

namespace SistemaVoto.Api.Services
{
    public sealed class VoteHashService
    {
        private readonly string _hmacKey;

        public VoteHashService(IConfiguration cfg)
        {
            _hmacKey = cfg["Security:VoteHmacKey"] ?? "DEV_ONLY_CHANGE_ME";
        }

        public string HashVote(string hashPrevio, int eleccionId, int? candidatoId, int? listaId, DateTime fechaUtc)
        {
            var payload = $"{eleccionId}|{candidatoId?.ToString() ?? ""}|{listaId?.ToString() ?? ""}|{fechaUtc:o}|{hashPrevio}";
            using var h = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacKey));
            return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }

        public string HashTransaccion(int eleccionId, int usuarioId, DateTime fechaUtc)
        {
            var payload = $"{eleccionId}|{usuarioId}|{fechaUtc:o}";
            using var h = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacKey));
            return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }
    }
}
