using dbRede.Models;
using Microsoft.AspNetCore.SignalR;

public class MensagensHub : Hub
{
    public async Task NovaMensagem(Mensagens mensagem)
    {
        // Lógica para enviar a mensagem ao destinatário
        await Clients.User(mensagem.id_destinatario.ToString()).SendAsync("NovaMensagem", mensagem);
    }
}
