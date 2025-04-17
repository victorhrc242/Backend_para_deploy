using Microsoft.AspNetCore.SignalR;

namespace dbRede.SignalR
{
    public class ComentarioHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"client Cinectado:{Context.ConnectionId}");
            return base.OnConnectedAsync();
        }
    }
}