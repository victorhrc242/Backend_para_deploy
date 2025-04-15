using dbRede.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace dbRede.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificacoesController : ControllerBase
    {
        private readonly Client _supabase;

        public NotificacoesController(IConfiguration configuration)
        {
            var service = new SupabaseService(configuration);
            _supabase = service.GetClient();
        }

        [HttpGet("{usuarioId}")]
        public async Task<IActionResult> GetNotificacoes(Guid usuarioId)
        {
            var resposta = await _supabase
                .From<Notificacao>()
                .Where(n => n.UsuarioId == usuarioId)
                .Order("data_envio", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            return Ok(new
            {
                usuarioId,
                total = resposta.Models.Count,
                notificacoes = resposta.Models
            });
        }

        [HttpPost]
        public async Task<IActionResult> CriarNotificacao([FromBody] NotificacaoDto dto)
        {
            var notificacao = new Notificacao
            {
                Id = Guid.NewGuid(),
                UsuarioId = dto.UsuarioId,
                Tipo = dto.Tipo,
                ReferenciaId = dto.ReferenciaId,
                Mensagem = dto.Mensagem,
                DataEnvio = DateTime.UtcNow
            };

            var resposta = await _supabase.From<Notificacao>().Insert(notificacao);

            return Ok(new
            {
                mensagem = "Notificação criada com sucesso.",
                notificacao = resposta.Models.FirstOrDefault()
            });
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Deletar(Guid id)
        {
            var resultado = await _supabase
                .From<Notificacao>()
                .Where(n => n.Id == id)
                .Single();

            if (resultado == null)
                return NotFound(new { erro = "Notificação não encontrada." });

            await _supabase.From<Notificacao>().Delete(resultado);

            return Ok(new
            {
                mensagem = "Notificação removida com sucesso.",
                idRemovido = id
            });
        }
        public class NotificacaoDto
        {
            public Guid UsuarioId { get; set; }
            public string Tipo { get; set; }
            public Guid ReferenciaId { get; set; }
            public string Mensagem { get; set; }
        }

    }
}
