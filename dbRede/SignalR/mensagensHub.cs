// Arquivo: Hubs/MensagensHub.cs

using Microsoft.AspNetCore.SignalR;

namespace dbRede.SignalR
{
    public class MensagensHub : Hub // <- Corrigido para PascalCase
    {
        public async Task NovaMensagem(string mensagem)
        {
            await Clients.All.SendAsync("ReceberMensagem", mensagem);
        }
    }
}
