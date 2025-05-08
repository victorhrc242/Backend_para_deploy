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
        public async Task NovaMensagem(string mensagem)
        {
            // Lógica para enviar a mensagem
            await Clients.All.SendAsync("ReceberMensagem", mensagem);
        }
    }
}
