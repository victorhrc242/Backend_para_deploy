using Microsoft.AspNetCore.SignalR;

public class MensagensHub : Hub
{
    public async Task ReceberMensagem(Mensagem mensagem)
    {
        // Validação de dados
        if (mensagem == null)
            throw new ArgumentNullException(nameof(mensagem));

        // Lógica para processar e salvar a mensagem
        // Exemplo: enviar para outro cliente
        await Clients.User(mensagem.idDestinatario).SendAsync("ReceberMensagem", mensagem);
    }
}

public class Mensagem
{
    public string idRemetente { get; set; }
    public string idDestinatario { get; set; }
    public string conteudo { get; set; }
}
