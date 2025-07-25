﻿using dbRede.Hubs;
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

    public ComentarioController(IConfiguration configuration, IHubContext<ComentarioHub> hubContext)
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
        // Criar notificação para o usuário que criou o post
        var notificacao = new Notificacao
        {
            Id = Guid.NewGuid(),
            UsuarioId = post.AutorId,  // Notificar o autor do post
            Tipo = "Comentario",
            UsuarioidRemetente = comentario.AutorId,  // Usuário que fez o comentário
            Mensagem = $"comentou no seu post", // Mensagem personalizada
            DataEnvio = DateTime.UtcNow
        };
        // Salvar a notificação
        await _supabase.From<Notificacao>().Insert(notificacao);
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
    [HttpGet("comentarios/{postId}")]
    public async Task<IActionResult> ListarComentariosPorPostId(Guid postId)
    {
        var resposta = await _supabase
            .From<Comentario>()
            .Where(c => c.PostId == postId)
            .Order("data_comentario", Ordering.Ascending)
            .Get();
        var comentarios = resposta.Models.Select(c => new
        {
            c.Id,
            c.PostId,
            c.AutorId,
            c.Conteudo,
            c.DataComentario,
        }).ToList();

        return Ok(new
        {
            sucesso = true,
            postId,
            comentarios
        });
    }
    //deletar
    [HttpDelete("{id}")]
    public async Task<IActionResult> Deletar(Guid id)
    {
        var resultado = await _supabase
            .From<Comentario>()
            .Where(n => n.Id == id)
            .Single();

        if (resultado == null)
            return NotFound(new { erro = "comentario não encontrada." });

        await _supabase.From<Comentario>().Delete(resultado);

        return Ok(new
        {
            mensagem = "comentario removida com sucesso.",
            idRemovido = id
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
