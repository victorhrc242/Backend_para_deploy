// Arquivo: Hubs/MensagensHub.cs

using Microsoft.AspNetCore.SignalR;

namespace dbRede.SignalR
{
    public class MensagensHub : Hub // <- Corrigido para PascalCase
    {
        public async Task NovaMensagem(string mensagem)
        {
            Console.WriteLine($"Nova mensagem recebida: {mensagem}");
            await Clients.All.SendAsync("ReceberMensagem", mensagem);
        }
    }
}
