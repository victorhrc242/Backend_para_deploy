using Microsoft.AspNetCore.Mvc;
using dbRede.Models;
using Supabase;
using static dbRede.Controllers.CurtidaController.CurtidaResponseDto;

namespace dbRede.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurtidaController : ControllerBase
    {
        private readonly Client _supabase;

        public CurtidaController(IConfiguration configuration)
        {
            var service = new SupabaseService(configuration);
            _supabase = service.GetClient();
        }

        [HttpPost("curtir")]
        public async Task<IActionResult> CurtirPost([FromBody] CriarCurtidaRequest request)
        {
            // 1. Criar a curtida
            var curtida = new Curtida
            {
                Id = Guid.NewGuid(),
                PostId = request.PostId,
                UsuarioId = request.UsuarioId,
                DataCurtiu = DateTime.UtcNow
            };

            var respostaCurtida = await _supabase.From<Curtida>().Insert(curtida);

            if (respostaCurtida == null || respostaCurtida.Models.Count == 0)
                return StatusCode(500, new { sucesso = false, mensagem = "Erro ao salvar curtida." });

            // 2. Buscar o post correspondente
            var respostaPost = await _supabase
                .From<Post>()
                .Where(p => p.Id == request.PostId)
                .Get();

            var post = respostaPost.Models.FirstOrDefault();

            if (post == null)
                return NotFound(new { sucesso = false, mensagem = "Post não encontrado." });

            // 3. Incrementar o número de curtidas
            post.Curtidas += 1;

            var respostaAtualizacao = await _supabase.From<Post>().Update(post);

            if (respostaAtualizacao == null || respostaAtualizacao.Models.Count == 0)
                return StatusCode(500, new { sucesso = false, mensagem = "Erro ao atualizar o número de curtidas." });

            // 4. Retornar resposta
            return Ok(new
            {
                sucesso = true,
                mensagem = "Curtida registrada com sucesso!",
                curtida = new
                {
                    curtida.Id,
                    curtida.PostId,
                    curtida.UsuarioId,
                    curtida.DataCurtiu
                },
                curtidasTotais = post.Curtidas
            });
        }
        // GET: api/curtida/post/{postId}
        [HttpGet("post/{postId}")]
        public async Task<IActionResult> ListarCurtidasPorPost(Guid postId)
        {
            var resposta = await _supabase
                .From<Curtida>()
                .Where(c => c.PostId == postId)
                .Get();

            var curtidas = resposta.Models.Select(c => new CurtidaResponseDto(c)).ToList();

            return Ok(new
            {
                sucesso = true,
                postId,
                total = curtidas.Count,
                curtidas
            });
        }
        public class CurtidaResponseDto
        {
            public Guid Id { get; set; }
            public Guid PostId { get; set; }
            public Guid UsuarioId { get; set; }
            public DateTime DataCurtiu { get; set; }

            public CurtidaResponseDto(Curtida curtida)
            {
                Id = curtida.Id;
                PostId = curtida.PostId;
                UsuarioId = curtida.UsuarioId;
                DataCurtiu = curtida.DataCurtiu;
            }
  
        public class CriarCurtidaRequest
        {
            public Guid PostId { get; set; }
            public Guid UsuarioId { get; set; }
        }
    
}
    }
}
