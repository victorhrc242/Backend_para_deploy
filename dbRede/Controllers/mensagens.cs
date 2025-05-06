using dbRede.Models;
using dbRede.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Supabase;

using static ComentarioController;
using static Supabase.Postgrest.Constants;

namespace dbRede.Controllers
{


    [ApiController]
    [Route("api/[controller]")]
    public class mensagens: ControllerBase
    {
        private readonly Client _supabase;
        private readonly IHubContext<mensagensHub> _HubContext;
        public mensagens(IConfiguration configuration, IHubContext<mensagensHub> hubContext)
        {
            var service = new SupabaseService(configuration);
            _supabase = service.GetClient();
            _HubContext = hubContext;
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
            await _HubContext.Clients.All.SendAsync("NovaMensagem", new
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
        // endpoint para listar as mensagens do chat
        [HttpGet("mensagens/{usuario1Id}/{usuario2Id}")]
        public async Task<IActionResult> ListarMensagensEntreUsuarios(Guid usuario1Id, Guid usuario2Id)
        {
            try
            {
                var remetente1 = usuario1Id.ToString();
                var destinatario1 = usuario2Id.ToString();
                var remetente2 = usuario2Id.ToString();
                var destinatario2 = usuario1Id.ToString();

                var resposta1 = await _supabase
                    .From<Mensagens>()
                    .Filter("id_remetente", Operator.Equals, remetente1)
                    .Filter("id_destinatario", Operator.Equals, destinatario1)
                    .Filter("apagada", Operator.Equals, "false") // corrigido
                    .Get();

                var resposta2 = await _supabase
                    .From<Mensagens>()
                    .Filter("id_remetente", Operator.Equals, remetente2)
                    .Filter("id_destinatario", Operator.Equals, destinatario2)
                    .Filter("apagada", Operator.Equals, "false") // corrigido
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
        // marcar as mensagens como apagada ao inves de simplesmente apagar
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
            // Notifica os clientes via SignalR que a mensagem foi apagada
            await _HubContext.Clients.All.SendAsync("MensagemApagada", mensagemId, mensagem.apagada);
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
        // PUT: api/mensagens/marcar-como-lida/{mensagemId}
        [HttpPut("marcar-como-lida/{mensagemId}")]
        public async Task<IActionResult> MarcarMensagemComoLida(Guid mensagemId)
        {
            // Obtendo a mensagem para garantir que ela exista
            var resposta = await _supabase
                .From<Mensagens>()
                .Where(m => m.Id == mensagemId)
                .Get();

            var mensagem = resposta.Models.FirstOrDefault();

            if (mensagem == null)
            {
                return NotFound(new { sucesso = false, mensagem = "Mensagem não encontrada." });
            }

            // Atualizando o campo 'lida' para true
            mensagem.lida = true;

            // Atualizando a mensagem no banco de dados
            var updateResposta = await _supabase
                .From<Mensagens>()
                .Update(mensagem);
            await _HubContext.Clients.All.SendAsync("Mensagem lida", mensagemId, mensagem.lida);
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
        // dtos
        public class EnviarMensagemRequest
        {
            public Guid IdRemetente { get; set; }
            public Guid IdDestinatario { get; set; }
            public string Conteudo { get; set; }
        }

    }
}
