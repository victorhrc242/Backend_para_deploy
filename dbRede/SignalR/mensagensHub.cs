// Arquivo: Hubs/MensagensHub.cs

using Microsoft.AspNetCore.SignalR;

namespace dbRede.SignalR
{
    public class MensagensHub : Hub // <- Corrigido para PascalCase
    {
        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"Cliente Conectado: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }
    }
}
