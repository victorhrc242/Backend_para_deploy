using dbRede.Hubs;
using dbRede.Models;
using dbRede.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Supabase;
using static ComentarioController;
using static dbRede.Controllers.CurtidaController;
using static dbRede.Controllers.FeedController;
using static Supabase.Postgrest.Constants;

[ApiController]
[Route("api/[controller]")]
public class ComentarioController : ControllerBase
{
    private readonly Client _supabase;
    private readonly IHubContext<ComentarioHub> _HubContext;

    public ComentarioController(IConfiguration configuration,IHubContext <ComentarioHub> hubContext)
    {
        var service = new SupabaseService(configuration);
        _supabase = service.GetClient();
        _HubContext = hubContext;
    }

    [HttpPost("comentar")]
    public async Task<IActionResult> Comentar([FromBody] CriarComentarioRequest request)
    {
        var comentario = new Comentario
        {
            Id = Guid.NewGuid(),
            PostId = request.PostId,
            AutorId = request.AutorId,
            Conteudo = request.Conteudo,
            DataComentario = DateTime.UtcNow
        };

        var resposta = await _supabase.From<Comentario>().Insert(comentario);

        if (resposta.Models.Count == 0)
            return StatusCode(500, new { sucesso = false, mensagem = "Erro ao salvar o comentário." });

        // Buscar o post correspondente
        var postResposta = await _supabase.From<Post>().Where(p => p.Id == request.PostId).Get();

        var post = postResposta.Models.FirstOrDefault();

        if (post != null)
        {
            // Incrementar a contagem de comentários
            post.Comentarios += 1;

            // Atualizar o post no banco
            await _supabase.From<Post>().Update(post);
        }
        await _HubContext.Clients.All.SendAsync("Novo comentario", post);
        return Ok(new
        {
            sucesso = true,
            mensagem = "Comentário salvo com sucesso!",
            comentario = new
            {
                comentario.Id,
                comentario.PostId,
                comentario.AutorId,
                comentario.Conteudo,
                comentario.DataComentario
            }
        });
    }
    [HttpGet("mensagens/{usuario1Id}/{usuario2Id}")]
    public async Task<IActionResult> ListarMensagensEntreUsuarios(Guid usuario1Id, Guid usuario2Id)
    {
        var resposta = await _supabase
            .From<Mensagens>()
            .Where(m =>
                (m.id_remetente == usuario1Id && m.id_destinatario == usuario2Id) ||
                (m.id_remetente == usuario2Id && m.id_destinatario == usuario1Id)
            )
            .Order("data_envio", Ordering.Ascending)
            .Get();

        var mensagens = resposta.Models.Select(m => new
        {
            m.Id,
            m.id_remetente,
            m.id_destinatario,
            m.conteudo,
            m.data_envio,
            m.lida,
            m.apagada,
        }).ToList();

        return Ok(new
        {
            sucesso = true,
            usuarios = new[] { usuario1Id, usuario2Id },
            mensagens
        });
    }

    public class CriarComentarioRequest
    {
        public Guid PostId { get; set; }
        public Guid AutorId { get; set; }
        public string Conteudo { get; set; }
    }

    public class ComentarioResponseDto
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public Guid AutorId { get; set; }
        public string Conteudo { get; set; }
        public DateTime DataComentario { get; set; }
        public string NomeAutor { get; set; }

        public ComentarioResponseDto() { }

        public ComentarioResponseDto(Comentario comentario)
        {
            Id = comentario.Id;
            PostId = comentario.PostId;
            AutorId = comentario.AutorId;
            Conteudo = comentario.Conteudo;
            DataComentario = comentario.DataComentario;
        }
    }


}
