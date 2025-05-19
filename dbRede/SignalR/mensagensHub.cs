using dbRede.Models;
using Microsoft.AspNetCore.SignalR;

public class MensagensHub : Hub
{
    // Método para enviar uma mensagem através do SignalR
    public async Task ReceberMensagem(Mensagens mensagem)
    {
        if (mensagem == null)
            throw new ArgumentNullException(nameof(mensagem));

        // Envia a mensagem para o destinatário específico
        await Clients.User(mensagem.id_destinatario.ToString()).SendAsync("ReceberMensagem", mensagem);
    }
}
