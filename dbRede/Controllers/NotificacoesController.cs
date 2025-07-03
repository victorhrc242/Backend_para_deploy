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
            try
            {
                // Consulta as notificações do Supabase para o usuário fornecido
                var resposta = await _supabase
                    .From<Notificacao>()
                    .Where(n => n.UsuarioId == usuarioId)
                    .Order("data_envio", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                // Mapeia os dados do modelo para o DTO
                var notificacoesDto = resposta.Models.Select(n => new NotificacaoDto
                {
                    Id = n.Id,
                    UsuarioId = n.UsuarioId,
                    UsuarioRemetenteId = n.UsuarioidRemetente, // <--- Remetente
                    Tipo = n.Tipo,
                    Mensagem = n.Mensagem,
                    DataEnvio = n.DataEnvio
                }).ToList();

                // Retorna os dados no formato JSON
                return Ok(new
                {
                    usuarioId,
                    total = notificacoesDto.Count,
                    notificacoes = notificacoesDto
                });
            }
            catch (Exception ex)
            {
                // Se ocorrer um erro, retorna uma resposta de erro
                return StatusCode(500, new { mensagem = "Erro ao buscar notificações", erro = ex.Message });
            }
        }   

        [HttpPost]
        public async Task<IActionResult> CriarNotificacao([FromBody] NotificacaoDto dto)
        {
            var notificacao = new Notificacao
            {
                Id = Guid.NewGuid(),
                UsuarioId = dto.UsuarioId,
                Tipo = dto.Tipo,
                ReferenciaId = dto.Id,
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
            public Guid Id { get; set; }
            public Guid UsuarioId { get; set; }
            public Guid? UsuarioRemetenteId { get; set; } // <--- Remetente
            public string Tipo { get; set; }
            public string Mensagem { get; set; }
            public DateTime DataEnvio { get; set; }
        }

    }
}
