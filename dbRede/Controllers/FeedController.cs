using Microsoft.AspNetCore.Mvc;
using dbRede.Models;
using Supabase;
using static Supabase.Postgrest.Constants;
using static dbRede.Controllers.FeedController;
using Microsoft.AspNetCore.SignalR;
using dbRede.Hubs;

namespace dbRede.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedController : ControllerBase
    {
        private readonly Client _supabase;
        private readonly IHubContext<FeedHub> _HubContext;
        public FeedController(IConfiguration configuration, IHubContext<FeedHub> hubContext)
        {
            var service = new SupabaseService(configuration);
            _supabase = service.GetClient();
            _HubContext = hubContext;
        }

        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed()
        {
            var resultado = await _supabase
                .From<Post>()
                .Select("*, users (nome)") // nome da tabela referenciada
                .Get();

            if (resultado == null)
                return StatusCode(500, new { erro = "Erro ao acessar o Supabase." });

            var postsComAutores = resultado.Models.Select(post => new PostDTO
            {
                Id = post.Id,
                Conteudo = post.Conteudo,
                Imagem = post.Imagem,
                Video=post.Video,
                Tags = post.Tags,
                DataPostagem = post.DataPostagem,
                Curtidas = post.Curtidas,
                Comentarios = post.Comentarios,
                AutorId = post.AutorId,
                NomeAutor = post.Usuarios?.Nome ?? "Desconhecido"
            });

            return Ok(postsComAutores);
        }
        // lista o post de quem o usuario esta seguindo
        [HttpGet("feed/{usuarioId}")]
        public async Task<IActionResult> GetFeed(Guid usuarioId)
        {
            // 1. Buscar os usuários que o usuário autenticado está seguindo com status "aceito"
            var seguindoResponse = await _supabase
                .From<Seguidor>()
                .Where(s => s.Usuario1 == usuarioId && s.Status == "aceito")
                .Get();

            if (seguindoResponse == null)
                return StatusCode(500, new { erro = "Erro ao acessar a lista de usuários seguidos." });

            var idsSeguidos = seguindoResponse.Models.Select(s => s.Usuario2).ToList();

            // 2. Se não estiver seguindo ninguém, retorna feed vazio
            if (!idsSeguidos.Any())
                return Ok(new List<PostDTO>());

            // 3. Buscar todos os posts ordenados por data mais recente
            var resultado = await _supabase
                .From<Post>()
                .Select("*, users (nome)")
                .Order("data_postagem", Ordering.Descending) // <- aqui está a correção
                .Get();

            if (resultado == null)
                return StatusCode(500, new { erro = "Erro ao acessar os posts no Supabase." });

            // 4. Filtrar apenas os posts dos usuários seguidos
            var postsComAutores = resultado.Models
                .Where(post => idsSeguidos.Contains(post.AutorId))
                .Select(post => new PostDTO
                {
                    Id = post.Id,
                    Conteudo = post.Conteudo,
                    Imagem = post.Imagem,
                    Video = post.Video,
                    Tags = post.Tags,
                    DataPostagem = post.DataPostagem,
                    Curtidas = post.Curtidas,
                    Comentarios = post.Comentarios,
                    AutorId = post.AutorId,
                    NomeAutor = post.Usuarios?.Nome ?? "Desconhecido"
                });

            return Ok(postsComAutores);
        }
        //lista o post em que o usuario fez
        [HttpGet("posts/usuario/{id}")]
        public async Task<IActionResult> ObterPostsPorUsuario(Guid id)
        {
            try
            {
                var resultado = await _supabase
                    .From<Post>()
                    .Select("*, users (nome)")
                    .Where(p => p.AutorId == id)
                    .Get();

                if (resultado == null)
                    return StatusCode(500, new { erro = "Erro ao acessar o Supabase." });

                var postsDoUsuario = resultado.Models.Select(post => new PostDTO
                {
                    Id = post.Id,
                    Conteudo = post.Conteudo,
                    Imagem = post.Imagem,
                    Video = post.Video,
                    Tags = post.Tags,
                    DataPostagem = post.DataPostagem,
                    Curtidas = post.Curtidas,
                    Comentarios = post.Comentarios,
                    AutorId = post.AutorId,
                    NomeAutor = post.Usuarios?.Nome ?? "Desconhecido"
                });

                return Ok(postsDoUsuario);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = "Erro interno ao buscar os posts do usuário.", detalhes = ex.Message });
            }
        }
        [HttpGet("feed-completo/{usuarioId}")]
        public async Task<IActionResult> GetFeedCompleto(Guid usuarioId)
        {
            int page = 1; int pageSize = 10;
            // 1. Buscar usuários seguidos
            var seguindo = await _supabase
                .From<Seguidor>()
                .Where(s => s.Usuario1 == usuarioId && s.Status == "aceito")
                .Get();

            var idsSeguidos = seguindo.Models.Select(s => s.Usuario2).ToList();

            // 2. Buscar curtidas e comentários
            var curtidas = await _supabase.From<Curtida>().Where(c => c.UsuarioId == usuarioId).Get();
            var comentarios = await _supabase.From<Comentario>().Where(c => c.AutorId == usuarioId).Get();

            // 3. Buscar posts interagidos
            var idsInteragidos = curtidas.Models.Select(c => c.PostId)
                .Concat(comentarios.Models.Select(c => c.PostId))
                .Distinct().ToList();

            var postsInteragidos = new List<Post>();
            if (idsInteragidos.Any())
            {
                var resp = await _supabase
                    .From<Post>()
                    .Filter("id", Operator.In, idsInteragidos)
                    .Get();

                postsInteragidos = resp.Models;
            }

            // 4. Extrair tags
            var tagsCurtidas = postsInteragidos
                .Where(p => p.Tags != null)
                .SelectMany(p => p.Tags)
                .Distinct()
                .ToList();

            // 5. Autores com mais interações
            var autoresInteragidos = curtidas.Models
                .Select(c => c.UsuarioId)
                .Concat(comentarios.Models.Select(c => c.AutorId))
                .Where(id => idsSeguidos.Contains(id))
                .GroupBy(id => id)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToList();

            // 6. Buscar todos os posts relevantes
            var todosPosts = await _supabase.From<Post>().Order("data_postagem", Ordering.Descending).Get();

            // 7. Agrupamento por tipo:
            var recentesSeguidos = todosPosts.Models
                .Where(p => idsSeguidos.Contains(p.AutorId) && p.DataPostagem >= DateTime.UtcNow.AddDays(-2))
                .OrderByDescending(p => autoresInteragidos.Contains(p.AutorId) ? 1 : 0)
                .ThenByDescending(p => p.DataPostagem);

            var recomendadosPorTags = todosPosts.Models
                .Where(p => !idsSeguidos.Contains(p.AutorId) &&
                            p.Tags != null &&
                            p.Tags.Any(tag => tagsCurtidas.Contains(tag)) &&
                            p.DataPostagem >= DateTime.UtcNow.AddDays(-2))
                .OrderByDescending(p => p.DataPostagem);

            var antigosInteragidos = postsInteragidos
                .Where(p => p.DataPostagem < DateTime.UtcNow.AddDays(-2))
                .OrderByDescending(p => p.DataPostagem);

            // 8. Junta tudo, remove duplicados e aplica paginação
            var feedCompleto = recentesSeguidos
                .Concat(recomendadosPorTags)
                .Concat(antigosInteragidos)
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 9. Carrega nomes dos autores
            var autorIds = feedCompleto.Select(p => p.AutorId).Distinct().ToList();
            var autores = await _supabase
                .From<User>()
                .Filter("id", Operator.In, autorIds)
                .Get();

            var mapaAutores = autores.Models.ToDictionary(u => u.id, u => u.Nome);

            // 10. Monta DTOs
            var resultado = feedCompleto.Select(post => new PostDTO
            {
                Id = post.Id,
                Conteudo = post.Conteudo,
                Imagem = post.Imagem,
                Video = post.Video,
                Tags = post.Tags,
                DataPostagem = post.DataPostagem,
                Curtidas = post.Curtidas,
                Comentarios = post.Comentarios,
                AutorId = post.AutorId,
                NomeAutor = mapaAutores.TryGetValue(post.AutorId, out var nome) ? nome : "Desconhecido"
            });

            return Ok(resultado);
        }



        [HttpPost("criar")]
        public async Task<IActionResult> CriarPost([FromBody] CriarPostRequest novoPost)
        {
            if (string.IsNullOrEmpty(novoPost.Imagem) && string.IsNullOrEmpty(novoPost.Video))
            {
                return BadRequest(new { erro = "Você deve fornecer uma imagem ou um vídeo." });
            }
            var post = new Post
            {
                Id = Guid.NewGuid(),
                AutorId = novoPost.AutorId,
                Conteudo = novoPost.Conteudo,
                Imagem = novoPost.Imagem,
                Video = novoPost.Video,
                DataPostagem = DateTime.UtcNow,
                Curtidas = 0,
                Comentarios = 0,
                Tags = novoPost.Tags,
            };

            var resposta = await _supabase.From<Post>().Insert(post);
            var postSalvo = resposta.Models.FirstOrDefault();

            if (postSalvo == null)
                return StatusCode(500, new { erro = "Erro ao salvar o post." });

            var resultado = await _supabase
                .From<Post>()
                .Select("*, users (nome)")
                .Filter("id", Operator.Equals, postSalvo.Id.ToString())
                .Get();

            var postComAutor = resultado.Models.FirstOrDefault();

            var dto = new PostDTO
            {
                Id = postComAutor.Id,
                AutorId = postComAutor.AutorId,
                Conteudo = postComAutor.Conteudo,
                Imagem = postComAutor.Imagem,
                Video = postComAutor.Video,
                DataPostagem = postComAutor.DataPostagem,
                Curtidas = postComAutor.Curtidas,
                Comentarios = postComAutor.Comentarios,
                Tags = postComAutor.Tags,
                NomeAutor = postComAutor.Usuarios?.Nome ?? "Desconhecido"
            };

            // ✅ Notifica todos os clientes conectados sobre o novo post
            await _HubContext.Clients.All.SendAsync("NovoPost", dto);

            return Ok(new
            {
                mensagem = "Post criado com sucesso!",
                post = dto
            });
        }


        // delete posts
        [HttpDelete("{id}")]
        public async Task<IActionResult>deletar(Guid id)
        {
            var resultado = await _supabase
                .From<Post>()
                .Where(n => n.Id == id)
                .Single();
            if(resultado == null)
                return NotFound(new { erro = "Post não encontrado" });
            await _supabase.From<Post>().Delete(resultado);

            return Ok(new
            {
                mensagem = "Post apagado com sucesso",
                idRemovido = id
            });
            
        }


        // começos de DTOS
        public class CriarPostRequest
        {
            public Guid AutorId { get; set; }
            public string Conteudo { get; set; }
            public string? Imagem { get; set; }
            public string? Video { get; set; }
            public List<string> Tags { get; set; }
        }

        public class PostDTO
        {
            public Guid Id { get; set; }
            public Guid AutorId { get; set; }
            public string Conteudo { get; set; }
            public string Imagem { get; set; }
            public string Video { get; set; }
            public DateTime DataPostagem { get; set; }
            public int Curtidas { get; set; }
            public int Comentarios { get; set; }
            public List<string> Tags { get; set; }
            public string NomeAutor { get; set; }
        }
    }
}