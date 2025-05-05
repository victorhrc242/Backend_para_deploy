using Microsoft.AspNetCore.SignalR;

namespace dbRede.SignalR
{
    public class mensagensHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"client Cinectado:{Context.ConnectionId}");
            return base.OnConnectedAsync();
        }
    }
}