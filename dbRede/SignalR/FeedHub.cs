using Microsoft.AspNetCore.SignalR;

namespace dbRede.Hubs
{
    public class FeedHub : Hub
    {
        // Método opcional para logar conexões
        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"Cliente conectado: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }
    }
}
