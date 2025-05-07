using dbRede.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Supabase;
using static Supabase.Postgrest.Constants;

[ApiController]
[Route("api/[controller]")]
public class MensagensController : ControllerBase
{
    private readonly Client _supabase;
    private readonly IHubContext<mensagensHub> _hubContext;

    public MensagensController(IConfiguration configuration, IHubContext<mensagensHub> hubContext)
    {
        var service = new SupabaseService(configuration);
        _supabase = service.GetClient();
        _hubContext = hubContext;
    }

    [HttpPost("enviar")]
    public async Task<IActionResult> Enviar([FromBody] EnviarMensagemRequest request)
    {
        var mensagem = new Mensagens
        {
            Id = Guid.NewGuid(),
            id_remetente = request.IdRemetente,
            id_destinatario = request.IdDestinatario,
            conteudo = request.Conteudo,
            data_envio = DateTime.UtcNow,
            lida = false,
            apagada = false
        };

        var resposta = await _supabase.From<Mensagens>().Insert(mensagem);

        if (resposta.Models.Count == 0)
            return StatusCode(500, new { sucesso = false, mensagem = "Erro ao enviar a mensagem." });

        // Notificar os clientes conectados em tempo real
        await _hubContext.Clients.User(request.IdDestinatario.ToString()).SendAsync("NovaMensagem", new
        {
            mensagem.Id,
            mensagem.id_remetente,
            mensagem.id_destinatario,
            mensagem.conteudo,
            mensagem.data_envio,
            mensagem.lida
        });

        return Ok(new
        {
            sucesso = true,
            mensagem = "Mensagem enviada com sucesso!",
            dados = new
            {
                mensagem.Id,
                mensagem.id_remetente,
                mensagem.id_destinatario,
                mensagem.conteudo,
                mensagem.data_envio,
                mensagem.lida
            }
        });
    }

    [HttpGet("mensagens/{usuario1Id}/{usuario2Id}")]
    public async Task<IActionResult> ListarMensagensEntreUsuarios(Guid usuario1Id, Guid usuario2Id)
    {
        try
        {
            var resposta1 = await _supabase
                .From<Mensagens>()
                .Filter("id_remetente", Operator.Equals, usuario1Id.ToString())
                .Filter("id_destinatario", Operator.Equals, usuario2Id.ToString())
                .Filter("apagada", Operator.Equals, "false")
                .Get();

            var resposta2 = await _supabase
                .From<Mensagens>()
                .Filter("id_remetente", Operator.Equals, usuario2Id.ToString())
                .Filter("id_destinatario", Operator.Equals, usuario1Id.ToString())
                .Filter("apagada", Operator.Equals, "false")
                .Get();

            var mensagens = resposta1.Models
                .Concat(resposta2.Models)
                .OrderBy(m => m.data_envio)
                .Select(m => new
                {
                    m.Id,
                    m.id_remetente,
                    m.id_destinatario,
                    m.conteudo,
                    m.data_envio,
                    m.lida,
                    m.apagada
                })
                .ToList();

            return Ok(new
            {
                sucesso = true,
                usuarios = new[] { usuario1Id, usuario2Id },
                mensagens
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                sucesso = false,
                mensagem = "Erro ao buscar mensagens.",
                erro = ex.Message
            });
        }
    }

    [HttpPut("mensagens/{mensagemId}/apagar")]
    public async Task<IActionResult> ApagarMensagem(Guid mensagemId)
    {
        var resposta = await _supabase
            .From<Mensagens>()
            .Where(m => m.Id == mensagemId)
            .Get();
        var mensagem = resposta.Models.FirstOrDefault();
        if (mensagem == null)
            return NotFound(new { sucesso = false, mensagem = "Mensagem não encontrada." });
        mensagem.apagada = true;
        await _supabase.From<Mensagens>().Update(mensagem);

        // Notificar os clientes via SignalR que a mensagem foi apagada
        await _hubContext.Clients.All.SendAsync("MensagemApagada", mensagemId);
        return Ok(new
        {
            sucesso = true,
            mensagem = "Mensagem marcada como apagada.",
            dados = new
            {
                mensagem.Id,
                mensagem.apagada
            }
        });
    }

    [HttpPut("marcar-como-lida/{mensagemId}")]
    public async Task<IActionResult> MarcarMensagemComoLida(Guid mensagemId)
    {
        var resposta = await _supabase
            .From<Mensagens>()
            .Where(m => m.Id == mensagemId)
            .Get();

        var mensagem = resposta.Models.FirstOrDefault();

        if (mensagem == null)
        {
            return NotFound(new { sucesso = false, mensagem = "Mensagem não encontrada." });
        }

        mensagem.lida = true;

        var updateResposta = await _supabase
            .From<Mensagens>()
            .Update(mensagem);
        await _hubContext.Clients.All.SendAsync("MensagemLida", mensagemId, mensagem.lida);
        return Ok(new
        {
            sucesso = true,
            mensagem = "Mensagem marcada como lida com sucesso.",
            dados = new
            {
                mensagem.Id,
                mensagem.lida
            }
        });
    }
    public class EnviarMensagemRequest
    {
        public Guid IdRemetente { get; set; }
        public Guid IdDestinatario { get; set; }
        public string Conteudo { get; set; }
    }
}
