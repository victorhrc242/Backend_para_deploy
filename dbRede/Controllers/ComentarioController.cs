using dbRede.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using static dbRede.Controllers.CurtidaController;
using static Supabase.Postgrest.Constants;

[ApiController]
[Route("api/[controller]")]
public class ComentarioController : ControllerBase
{
    private readonly Client _supabase;

    public ComentarioController(IConfiguration configuration)
    {
        var service = new SupabaseService(configuration);
        _supabase = service.GetClient();
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
    // GET: api/comentario/post/{postId}
    [HttpGet("post/{postId}")]
    public async Task<IActionResult> ListarComentariosPorPost(Guid postId)
    {
        var resposta = await _supabase
            .From<Comentario>()
            .Where(c => c.PostId == postId)
            .Order("data_comentario", Ordering.Ascending)
            .Get();

        var comentarios = resposta.Models.Select(c => new ComentarioResponseDto(c)).ToList();

        return Ok(new
        {
            sucesso = true,
            postId,
            comentarios
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
