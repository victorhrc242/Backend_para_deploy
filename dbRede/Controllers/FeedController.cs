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
        // esse lista tudo pelo jeito
        [HttpGet("feed-completo/{usuarioId}")]
        public async Task<IActionResult> GetFeedCompleto(Guid usuarioId)
        {
            // 1. Buscar os usuários que o usuário segue
            var seguindoResponse = await _supabase
                .From<Seguidor>()
                .Where(s => s.Usuario1 == usuarioId && s.Status == "aceito")
                .Get();

            var idsSeguidos = seguindoResponse.Models.Select(s => s.Usuario2).ToList();

            // 2. Buscar posts de todos os usuários seguidos
            var postsSeguidos = new List<Post>();
            if (idsSeguidos.Any())
            {
                var respostaPosts = await _supabase
                    .From<Post>()
                    .Filter("autor_id", Supabase.Postgrest.Constants.Operator.In, idsSeguidos)
                    .Order("data_postagem", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                postsSeguidos = respostaPosts.Models;
            }

            // 3. Buscar tags dos posts curtidos pelo usuário
            var curtidas = await _supabase
                .From<Curtida>()
                .Where(c => c.UsuarioId == usuarioId)
                .Get();

            var postIdsCurtidos = curtidas.Models.Select(c => c.PostId).Distinct().ToList();

            var postsCurtidos = new List<Post>();
            if (postIdsCurtidos.Any())
            {
                var respostaPostsCurtidos = await _supabase
                    .From<Post>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.In, postIdsCurtidos)
                    .Get();

                var tagsCurtidas = respostaPostsCurtidos.Models
                    .Where(p => p.Tags != null)
                    .SelectMany(p => p.Tags)
                    .Distinct()
                    .ToList();

                if (tagsCurtidas.Any())
                {
                    var respostaPostsPorTag = await _supabase
                        .From<Post>()
                        .Get();

                    postsCurtidos = respostaPostsPorTag.Models
                        .Where(p => p.Tags != null && p.Tags.Any(tag => tagsCurtidas.Contains(tag)))
                        .ToList();
                }
            }

            // 4. Combinar os resultados (sem duplicatas)
            var todosPosts = postsSeguidos
                .Concat(postsCurtidos)
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .OrderByDescending(p => p.DataPostagem)
                .ToList();

            // 5. Montar DTOs com nome do autor
            var postsComAutores = todosPosts.Select(post => new PostDTO
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