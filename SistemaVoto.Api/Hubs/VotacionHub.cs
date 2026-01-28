using Microsoft.AspNetCore.SignalR;

namespace SistemaVoto.Api.Hubs
{
    public class VotacionHub : Hub
    {
        public Task JoinEleccion(int eleccionId) =>
            Groups.AddToGroupAsync(Context.ConnectionId, $"eleccion-{eleccionId}");
    }
}
