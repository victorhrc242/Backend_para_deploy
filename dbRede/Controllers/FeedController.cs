using Microsoft.AspNetCore.Mvc;
using dbRede.Models;
using Supabase;
using static Supabase.Postgrest.Constants;
using static dbRede.Controllers.FeedController;
using Microsoft.AspNetCore.SignalR;
using dbRede.Hubs;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using Supabase;
using System.Text;
using Microsoft.ML;

namespace dbRede.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedController : ControllerBase
    {
        private readonly Client _supabase;
        private readonly IHubContext<FeedHub> _HubContext;
        private readonly IMemoryCache _cache;
        private readonly PredictionEngine<ModeloInput, ModeloOutput> _predictionEngine;
        public FeedController(IConfiguration configuration, IHubContext<FeedHub> hubContext, IMemoryCache cache)
        {
            var service = new SupabaseService(configuration);
            _supabase = service.GetClient();
            _HubContext = hubContext;
            _cache = cache;
            var mlContext = new MLContext();
            DataViewSchema modelSchema;
            var modeloTreinado = mlContext.Model.Load("modelo_feed.zip", out modelSchema);

            _predictionEngine = mlContext.Model.CreatePredictionEngine<ModeloInput, ModeloOutput>(modeloTreinado);
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


        [HttpGet("feed-porID/{id}")]
        public async Task<IActionResult> GetPostPorId(Guid id)
        {
            var resultado = await _supabase
                .From<Post>()
                .Select("*, users (nome)")
                .Filter("id", Operator.Equals, id.ToString()) // 👈 Aqui está a correção
                .Get();

            if (resultado == null || resultado.Models.Count == 0)
                return NotFound(new { erro = "Post não encontrado." });

            var post = resultado.Models.First();

            var postDTO = new PostDTO
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
            };

            return Ok(postDTO);
        }



        // lista o post de quem o usuario esta seguindo
        [HttpGet("feed/{usuarioId}")]
        public async Task<IActionResult> GetFeed(Guid usuarioId)
        {
            // paginação 
            int page = 1; int pagesize = 5;

            page = page < 1 ? 1 : page;
            pagesize = pagesize < 1 ? 5 : pagesize;
            int ofset = (page - 1) * pagesize;
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
            //   colocando a paginação na busca 
                 .Filter("autor_id", Operator.In, idsSeguidos)
                .Range(ofset, ofset + pagesize - 1)
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
        [HttpGet("feed-dinamico-algoritimo-home/{usuarioId}")]
        public async Task<IActionResult> GetFeedCompleto(Guid usuarioId, int page = 1, int pageSize = 10)
        {
            var stopwatchs = Stopwatch.StartNew();
            string cacheKey = $"feed_{usuarioId}_p{page}_s{pageSize}";

            if (_cache.TryGetValue(cacheKey, out List<PostDTO> resultadoCache))
            {
                Console.WriteLine("⚡ Feed retornado do cache.");
                return Ok(resultadoCache);
            }

            var seguindoTask = _cache.GetOrCreateAsync($"seg_{usuarioId}", async _ =>
                (await _supabase.From<Seguidor>().Where(s => s.Usuario1 == usuarioId && s.Status == "aceito").Get()).Models);

            var curtidasTask = _cache.GetOrCreateAsync($"curt_{usuarioId}", async _ =>
                (await _supabase.From<Curtida>().Where(c => c.UsuarioId == usuarioId).Get()).Models);

            var comentariosTask = _cache.GetOrCreateAsync($"com_{usuarioId}", async _ =>
                (await _supabase.From<Comentario>().Where(c => c.AutorId == usuarioId).Get()).Models);

            var postsTask = _supabase
                .From<Post>()
                .Order("data_postagem", Ordering.Descending)
                .Get();

            var visualizacoesTask = _cache.GetOrCreateAsync($"viz_{usuarioId}", async _ =>
                (await _supabase.From<VisualizacaoPost>().Where(v => v.usuario_id == usuarioId).Get()).Models);

            await Task.WhenAll(seguindoTask, curtidasTask, comentariosTask, postsTask, visualizacoesTask);

            var seguindo = seguindoTask.Result;
            var curtidas = curtidasTask.Result;
            var comentarios = comentariosTask.Result;
            var posts = postsTask.Result.Models;
            var visualizacoesUsuario = visualizacoesTask.Result;

            var idsSeguidos = seguindo.Select(s => s.Usuario2).ToHashSet();
            var idsInteragidos = curtidas.Select(c => c.PostId).Concat(comentarios.Select(c => c.PostId)).ToHashSet();

            List<string> tagsCurtidas = new();
            if (idsInteragidos.Any())
            {
                var interagidosResp = await _supabase
                    .From<Post>()
                    .Filter("id", Operator.In, idsInteragidos.ToList())
                    .Get();

                tagsCurtidas = interagidosResp.Models
                    .Where(p => p.Tags != null)
                    .SelectMany(p => p.Tags)
                    .Distinct()
                    .ToList();
            }

            var mapaVisualizacaoUsuario = visualizacoesUsuario
                .GroupBy(v => v.post_id)
                .ToDictionary(g => g.Key, g => g.Count());

            // IA
            var stopwatchIA = Stopwatch.StartNew();
            var mlContext = new MLContext();
            var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "modelo_feed.zip");

            ITransformer modeloML;
            DataViewSchema modeloSchema;
            using (var fileStream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                modeloML = mlContext.Model.Load(fileStream, out modeloSchema);

            var predictionEngine = mlContext.Model.CreatePredictionEngine<ModeloInput, ModeloOutput>(modeloML);

            var resultadoIA = posts.Select((p, idx) =>
            {
                var entrada = new ModeloInput
                {
                    CurtidasEmComum = curtidas.Count(c => c.PostId == p.Id),
                    TagsEmComum = p.Tags?.Count(tag => tagsCurtidas.Contains(tag)) ?? 0,
                    EhSeguidor = idsSeguidos.Contains(p.AutorId) ? 1 : 0,
                    Recente = (DateTime.UtcNow - p.DataPostagem).TotalDays < 2 ? 1 : 0,
                    JaVisualizou = mapaVisualizacaoUsuario.ContainsKey(p.Id) ? 1 : 0,
                    TempoVisualizacaoUsuario = mapaVisualizacaoUsuario.TryGetValue(p.Id, out var t) ? t : 0,
                    TotalVisualizacoesPost = p.visualizacao ?? 0
                };

                var resultado = predictionEngine.Predict(entrada);
                return new ScoreResponse { postId = idx, score = resultado.Probability };
            }).ToList();

            var mapaScores = resultadoIA.ToDictionary(x => x.postId, x => x.score);
            stopwatchIA.Stop();
            Console.WriteLine($"⚙️ IA executada em: {stopwatchIA.ElapsedMilliseconds} ms");

            // Preparar HashSet com posts curtidos pelo usuário para consulta rápida
            var postsCurtidosUsuario = curtidas.Select(c => c.PostId).ToHashSet();

            // Ordenação + Paginação
            var feedFiltrado = posts
                .Where((p, idx) => mapaScores.ContainsKey(idx))
                .OrderBy(p => mapaVisualizacaoUsuario.ContainsKey(p.Id) ? 1 : 0)
                .ThenByDescending(p => mapaScores[posts.IndexOf(p)])
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var autorIds = feedFiltrado.Select(p => p.AutorId).Distinct().ToList();

            var autoresResp = await _supabase
                .From<User>()
                .Filter("id", Operator.In, autorIds)
                .Get();

            var mapaAutores = autoresResp.Models.ToDictionary(u => u.id, u => u.Nome);

            var resultado = feedFiltrado.Select(post => new PostDTO
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
                NomeAutor = mapaAutores.TryGetValue(post.AutorId, out var nome) ? nome : "Desconhecido",
                visualization = post.visualizacao,
                FoiCurtido = postsCurtidosUsuario.Contains(post.Id) // <-- campo novo
            }).ToList();

            stopwatchs.Stop();
            Console.WriteLine($"🕒 Endpoint total executado em: {stopwatchs.ElapsedMilliseconds} ms");

            _cache.Set(cacheKey, resultado, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(10)));

            return Ok(resultado);
        }


        //   visualização sendo contada    e salvada  para o algoritimo filtrar melhor o feed  

        [HttpPost("post/{postId}/visualizacao")]
        public async Task<IActionResult> RegistrarVisualizacao(Guid postId, [FromQuery] Guid usuarioId, [FromQuery] int tempoEmSegundos = 0)
        {
            if (tempoEmSegundos < 2)
            {
                return BadRequest(new { erro = "Tempo de visualização insuficiente." });
            }

            // Busca visualizações anteriores do usuário para esse post
            var visualizacoes = await _supabase
           .From<VisualizacaoPost>()
           .Where(v => v.usuario_id == usuarioId && v.post_id == postId)
           .Get();

            // Validação: já visualizou nos últimos 5 minutos?
            if (visualizacoes.Models.Any())
            {
                var ultimaVisualizacao = visualizacoes.Models
                    .OrderByDescending(v => v.data_visualizacao)
                    .First();

                var tempoDecorrido = DateTime.UtcNow - ultimaVisualizacao.data_visualizacao;

                if (tempoDecorrido.TotalMinutes < 5)
                {
                    return Ok(new { mensagem = "Visualização já registrada recentemente." });
                }
            }
            // Salva nova visualização
            await _supabase.From<VisualizacaoPost>().Insert(new VisualizacaoPost
            {
                usuario_id = usuarioId,
                post_id = postId,
                data_visualizacao = DateTime.UtcNow
            });

            // Atualiza contagem de visualizações no post
            var postResp = await _supabase.From<Post>().Where(p => p.Id == postId).Get();
            if (!postResp.Models.Any())
            {
                return NotFound(new { erro = "Post não encontrado." });
            }

            var postParaAtualizar = postResp.Models[0];
            postParaAtualizar.visualizacao = (postParaAtualizar.visualizacao ?? 0) + 1;

            var updateResp = await _supabase
                .From<Post>()
                .Where(p => p.Id == postId)
                .Update(postParaAtualizar);

            if (!updateResp.Models.Any())
            {
                return StatusCode(500, new { erro = "Falha ao atualizar visualizações do post." });
            }

            return Ok(new { mensagem = "Visualização registrada com sucesso." });
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
        public async Task<IActionResult> deletar(Guid id)
        {
            var resultado = await _supabase
                .From<Post>()
                .Where(n => n.Id == id)
                .Single();
            if (resultado == null)
                return NotFound(new { erro = "Post não encontrado" });
            await _supabase.From<Post>().Delete(resultado);

            return Ok(new
            {
                mensagem = "Post apagado com sucesso",
                idRemovido = id
            });

        }

        [HttpGet("videos")]
        public async Task<IActionResult> GetVideos([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            int start = (page - 1) * pageSize;
            int end = start + pageSize - 1;

            // Buscar o total (count) de posts com vídeo
            var totalPosts = await _supabase
            .From<Post>()
            .Filter("video", Operator.NotEqual, (string?)null)
            .Filter("video", Operator.NotEqual, "")
            .Count(CountType.Exact);




            // Buscar os posts paginados com autor
            var resultado = await _supabase
                .From<Post>()
                .Filter("video", Operator.NotEqual, (string?)null)
                .Filter("video", Operator.NotEqual, "")
                .Select("*, users (nome)")
                .Range(start, end)
                .Get();

            if (resultado == null)
                return StatusCode(500, new { erro = "Erro ao acessar os posts com vídeo no Supabase." });

            var brasilTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");

            var videosComAutores = resultado.Models
                .Select(post => new PostDTO
                {
                    Id = post.Id,
                    Conteudo = post.Conteudo,
                    Video = post.Video,
                    Tags = post.Tags,
                    DataPostagem = TimeZoneInfo.ConvertTimeFromUtc(post.DataPostagem, brasilTimeZone),
                    Curtidas = post.Curtidas,
                    Comentarios = post.Comentarios,
                    AutorId = post.AutorId,
                    NomeAutor = post.Usuarios?.Nome ?? "Desconhecido"
                }).ToList();

            return Ok(new
            {
                videos = videosComAutores,
                total = totalPosts
            });
        }



        //  listar tags
        [HttpGet("tags")]
        public async Task<IActionResult> listar_tags([FromQuery] string busca = null)
        {
            // executa a query e faz a busca das tags 
            var resultado = await _supabase
                .From<Post>()
                .Select("tags")
                .Get();
            if (resultado == null)
                return StatusCode(500, new { erro = "não conseguimos listar as tags" });
            var todasastags = resultado.Models
                .Where(p => p.Tags != null)
                .SelectMany(p => p.Tags);



            // fz a buscsa casesensitive
            if (!string.IsNullOrEmpty(busca))
                todasastags = todasastags.Where(tag => tag.Contains(busca));


            // faz a lista das tags formatada para json
            var lista = todasastags
                .Distinct()
                .OrderBy(tag => tag)
                .ToList();
            return Ok(lista);

        }
        //private async Task<List<(Guid postId, double score)>> ObterPontuacoesIA(List<Post> posts, Guid usuarioId)
        //{
        //    try
        //    {
        //        var entradasIA = posts.Select(p => new
        //        {
        //            id = p.Id,
        //            curtidas_em_comum = p.Curtidas,
        //            tags_em_comum = p.Tags?.Count ?? 0,
        //            eh_seguidor = 1,
        //            recente = (DateTime.UtcNow - p.DataPostagem).TotalDays < 2 ? 1 : 0
        //        }).ToList();

        //        var jsonPath = Path.GetTempFileName();
        //        await System.IO.File.WriteAllTextAsync(jsonPath, System.Text.Json.JsonSerializer.Serialize(entradasIA));

        //        var scriptPath = @"C:\Users\PC\Documents\GitHub\Backend_para_deploy\dbRede\prever_feed.py";

        //        var psi = new ProcessStartInfo
        //        {
        //            FileName = "python", // ou caminho completo do python.exe se não estiver no PATH
        //            Arguments = $"\"{scriptPath}\" \"{jsonPath}\"",
        //            RedirectStandardOutput = true,
        //            RedirectStandardError = true,  // para capturar erros do python
        //            UseShellExecute = false,
        //            CreateNoWindow = true
        //        };

        //        using var process = Process.Start(psi);

        //        if (process == null)
        //            throw new Exception("Não foi possível iniciar o processo python.");

        //        string output = await process.StandardOutput.ReadToEndAsync();
        //        string error = await process.StandardError.ReadToEndAsync();

        //        process.WaitForExit();

        //        if (process.ExitCode != 0)
        //        {
        //            // Lança exceção com o erro do script python para você saber o que deu errado
        //            throw new Exception($"Erro no script Python: {error}");
        //        }

        //        var resultado = System.Text.Json.JsonSerializer.Deserialize<List<IAResposta>>(output);

        //        return resultado.Select(r => (r.postId, r.score)).ToList();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Erro ao executar script Python: " + ex.Message);
        //    }
        //}


        // começos do DTOS
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
            public int? visualization { get; set; }
            public bool FoiCurtido { get; set; }
        }
        public class ModeloInput
        {
            public float CurtidasEmComum { get; set; }
            public float TagsEmComum { get; set; }
            public float EhSeguidor { get; set; }
            public float Recente { get; set; }
            public float JaVisualizou { get; set; }
            public float TempoVisualizacaoUsuario { get; set; }
            public float TotalVisualizacoesPost { get; set; }
        }

        public class ModeloOutput
        {
            public float Probability { get; set; }  // usado se for classificação binária
            public float Score { get; set; }
        }

        public class IAResposta
        {
            public Guid postId { get; set; }
            public double score { get; set; }
        }
        public class PostEntrada
        {
            public int curtidas_em_comum { get; set; }
            public int tags_em_comum { get; set; }
            public int eh_seguidor { get; set; }
            public int recente { get; set; }
            public int ja_visualizou { get; set; }
            public int tempo_visualizacao_usuario { get; set; }
            public int total_visualizacoes_post { get; set; }
        }

        public class ScoreResponse
        {
            public int postId { get; set; }
            public float score { get; set; }
        }

    }
}