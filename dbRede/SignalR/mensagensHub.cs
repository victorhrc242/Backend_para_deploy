using dbRede.Models;
using Microsoft.AspNetCore.SignalR;

public class MensagensHub : Hub
{
    // Método para enviar uma mensagem através do SignalR
    // Enviar uma nova mensagem para o destinatário
    public async Task EnviarMensagem(Mensagens mensagem)
    {
        if (mensagem == null)
            throw new ArgumentNullException(nameof(mensagem));

        // Notifica o destinatário que recebeu uma nova mensagem
        await Clients.User(mensagem.id_destinatario.ToString())
            .SendAsync("NovaMensagem", new
            {
                mensagem.Id,
                mensagem.id_remetente,
                mensagem.id_destinatario,
                mensagem.conteudo,
                mensagem.data_envio,
                mensagem.lida
            });
    }

    // Marcar mensagem como lida e notificar todos (ou pode ser só destinatário)
    public async Task MarcarComoLida(Guid mensagemId)
    {
        // Notifica que a mensagem foi marcada como lida
        await Clients.All.SendAsync("MensagemLida", mensagemId, true);
    }

    // Apagar mensagem e notificar todos
    public async Task ApagarMensagem(Guid mensagemId)
    {
        // Notifica que a mensagem foi apagada
        await Clients.All.SendAsync("MensagemApagada", mensagemId);
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Usuário conectado: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        Console.WriteLine($"Usuário desconectado: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}
