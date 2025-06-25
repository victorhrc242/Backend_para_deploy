using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class CurtidaHub : Hub
{
    // Método chamado quando um usuário clica em curtir/descurtir um post.
    public async Task CurtirPost(string postId, string usuarioId)
    {
        // Lógica para curtir um post (pode ser interagido com o banco de dados aqui).
        // Por exemplo, adicionar ou remover a curtida do banco de dados.
        // Aqui apenas estamos notificando todos os clientes conectados sobre o evento.

        // Enviar uma mensagem para todos os clientes conectados (inclusive o que fez a ação).
        await Clients.All.SendAsync("ReceberCurtida", postId, usuarioId, true); // true indica que foi uma curtida.
    }

    // Método chamado quando um usuário descurte um post.
    public async Task DescurtirPost(string postId, string usuarioId)
    {
        // Lógica para descurtir um post (remover a curtida no banco de dados).
        // Aqui também, estamos apenas notificando os clientes conectados.

        // Enviar uma mensagem para todos os clientes conectados (inclusive o que fez a ação).
        await Clients.All.SendAsync("ReceberCurtida", postId, usuarioId, false); // false indica que foi um descurtir.
    }
}
