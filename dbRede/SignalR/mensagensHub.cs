using Microsoft.AspNetCore.SignalR;

public class mensagensHub : Hub
{
    public async Task EnviarMensagem(Guid remetenteId, Guid destinatarioId, string conteudo)
    {
        await Clients.User(destinatarioId.ToString()).SendAsync("NovaMensagem", new
        {
            remetenteId,
            destinatarioId,
            conteudo,
            data_envio = DateTime.UtcNow
        });
    }
}
