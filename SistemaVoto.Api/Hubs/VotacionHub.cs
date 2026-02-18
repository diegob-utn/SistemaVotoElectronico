using Microsoft.AspNetCore.SignalR;

namespace SistemaVoto.Api.Hubs
{
    public class VotacionHub : Hub
    {
        public async Task JoinEleccion(string eleccionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"eleccion-{eleccionId}");
            Console.WriteLine($"[SignalR] Client {Context.ConnectionId} joined group eleccion-{eleccionId}");
        }
    }
}
