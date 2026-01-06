namespace SistemaVoto.Api.Services
{
    public enum MetodoEscanos { Dhondt = 0, Webster = 1 }

    public sealed class SeatAllocationService
    {
        public Dictionary<int, int> AsignarEscanos(IReadOnlyDictionary<int, int> votosPorLista, int numEscanos, MetodoEscanos metodo)
        {
            var res = votosPorLista.Keys.ToDictionary(k => k, _ => 0);
            if (numEscanos <= 0 || votosPorLista.Count == 0) return res;

            var cocientes = new List<(int listaId, double q, int votos, int tieId)>();

            foreach (var (listaId, votos) in votosPorLista)
            {
                for (int i = 0; i < numEscanos; i++)
                {
                    var div = metodo == MetodoEscanos.Dhondt ? (i + 1) : (i * 2 + 1); // Webster: impares
                    cocientes.Add((listaId, (double)votos / div, votos, listaId));
                }
            }

            foreach (var t in cocientes
                .OrderByDescending(x => x.q)
                .ThenByDescending(x => x.votos)
                .ThenBy(x => x.tieId)
                .Take(numEscanos))
            {
                res[t.listaId]++;
            }

            return res;
        }
    }
}
