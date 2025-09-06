using Microsoft.AspNetCore.Mvc;
using dbRede.Models;
using Supabase;
using static dbRede.Controllers.CurtidaController.CurtidaResponseDto;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using static MensagensController;
using StackExchange.Redis;

namespace dbRede.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurtidaController : ControllerBase
    {
        private readonly Client _supabase;
        private readonly IHubContext<CurtidaHub> _hubContext;
        private readonly IMongoCollection<Notificacao> _notificacoesCollection;
        private readonly IDatabase _redis;
        public CurtidaController(IConfiguration configuration, IHubContext<CurtidaHub> hubContext,IConnectionMultiplexer redis)
        {
            var service = new SupabaseService(configuration);
            _supabase = service.GetClient();
            _hubContext = hubContext;
            var mongoSettings = configuration.GetSection("MongoSettings");
            var connectionString = mongoSettings.GetValue<string>("ConnectionString");
            var databaseName = mongoSettings.GetValue<string>("DatabaseName");
            _redis = redis.GetDatabase();

            // Configura MongoClient com TLS 1.2
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.SslSettings = new SslSettings
            {
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
            };

            var mongoClient = new MongoClient(settings);
            var mongoDatabase = mongoClient.GetDatabase(databaseName);
            _notificacoesCollection = mongoDatabase.GetCollection<Notificacao>("Notificacao");
        }

        [HttpPost("curtir")]
        public async Task<IActionResult> CurtirPost([FromBody] CriarCurtidaRequest request)
        {
            // 1. Criar a curtida (Supabase ainda é o "banco oficial")
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

            // 3. Incrementar curtidas no Redis
            await _redis.StringIncrementAsync($"post:{request.PostId}:likes");

            // Também incrementa no banco oficial (Supabase) para manter sincronizado
            post.Curtidas += 1;
            var respostaAtualizacao = await _supabase.From<Post>().Update(post);

            if (respostaAtualizacao == null || respostaAtualizacao.Models.Count == 0)
                return StatusCode(500, new { sucesso = false, mensagem = "Erro ao atualizar o número de curtidas." });

            // 4. Notificar todos os clientes conectados via SignalR
            await _hubContext.Clients.All.SendAsync("ReceberCurtida", request.PostId, request.UsuarioId, true);

            // 5. Criar notificação (Mongo)
            var notificacao = new Notificacao
            {
                UsuarioId = post.AutorId.ToString(),          // Autor do post recebe
                Tipo = "Curtida",
                UsuarioRemetenteId = curtida.UsuarioId.ToString(), // Quem curtiu
                Mensagem = "Curtiu seu post",
                DataEnvio = DateTime.UtcNow
            };

            await _notificacoesCollection.InsertOneAsync(notificacao);

            // 6. Retornar resposta
            // Busca curtidas atuais direto do Redis (mais rápido que contar no Supabase)
            var totalLikesRedis = await _redis.StringGetAsync($"post:{request.PostId}:likes");

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
                curtidasTotais = totalLikesRedis.HasValue ? (int)totalLikesRedis : post.Curtidas
            });
        }

        // GET: api/curtida/post/{postId}
        [HttpGet("post/{postId}")]
        public async Task<IActionResult> ListarCurtidasPorPost(Guid postId)
        {
            // Primeiro tenta pegar do Redis
            var totalLikesRedis = await _redis.StringGetAsync($"post:{postId}:likes");

            int totalCurtidas;
            List<CurtidaResponseDto> curtidas = new();

            if (totalLikesRedis.HasValue)
            {
                // Se tiver no Redis, só devolve o número
                totalCurtidas = (int)totalLikesRedis;
            }
            else
            {
                // Se não houver no Redis, busca no Supabase
                var resposta = await _supabase
                    .From<Curtida>()
                    .Where(c => c.PostId == postId)
                    .Get();

                curtidas = resposta.Models.Select(c => new CurtidaResponseDto(c)).ToList();
                totalCurtidas = curtidas.Count;

                // Salva no Redis para próximas consultas
                await _redis.StringSetAsync($"post:{postId}:likes", totalCurtidas);
            }

            return Ok(new
            {
                sucesso = true,
                postId,
                total = totalCurtidas,
                curtidas
            });
        }

        [HttpPost("descurtir")]
        public async Task<IActionResult> DescurtirPost([FromBody] CriarCurtidaRequest request)
        {
            // 1. Buscar a curtida existente no Supabase
            var curtidaExistente = await _supabase
                .From<Curtida>()
                .Where(c => c.PostId == request.PostId && c.UsuarioId == request.UsuarioId)
                .Get();

            var curtida = curtidaExistente.Models.FirstOrDefault();

            if (curtida == null)
                return NotFound(new { sucesso = false, mensagem = "Curtida não encontrada para este usuário no post." });

            // 2. Remover a curtida
            var respostaRemocao = await _supabase.From<Curtida>().Delete(curtida);

            if (respostaRemocao == null || respostaRemocao.Models.Count == 0)
                return StatusCode(500, new { sucesso = false, mensagem = "Erro ao remover curtida." });

            // 3. Atualizar o Redis (decrementar curtidas)
            var novasCurtidas = await _redis.StringDecrementAsync($"post:{request.PostId}:likes");
            if (novasCurtidas < 0)
            {
                // Garante que não fique negativo
                await _redis.StringSetAsync($"post:{request.PostId}:likes", 0);
                novasCurtidas = 0;
            }

            // 4. Atualizar no Supabase (banco oficial)
            var respostaPost = await _supabase
                .From<Post>()
                .Where(p => p.Id == request.PostId)
                .Get();

            var post = respostaPost.Models.FirstOrDefault();

            if (post == null)
                return NotFound(new { sucesso = false, mensagem = "Post não encontrado." });

            post.Curtidas = (int)novasCurtidas;

            var respostaAtualizacao = await _supabase.From<Post>().Update(post);

            if (respostaAtualizacao == null || respostaAtualizacao.Models.Count == 0)
                return StatusCode(500, new { sucesso = false, mensagem = "Erro ao atualizar o número de curtidas." });

            // 5. Notificar via SignalR
            await _hubContext.Clients.All.SendAsync("ReceberCurtida", request.PostId, request.UsuarioId, false);

            // 6. Retornar resposta
            return Ok(new
            {
                sucesso = true,
                mensagem = "Curtida removida com sucesso.",
                curtidasTotais = novasCurtidas
            });
        }

        //delete
        /// <summary>
        /// usado internamente
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> deletar(Guid id)
        {
            var resultado = await _supabase
                .From<Curtida>()
                .Where(n => n.Id == id)
                .Single();
            if (resultado == null)
                return NotFound(new { erro = "Curtida não encontrada." });
            await _supabase.From<Curtida>().Delete(resultado);
            return Ok(new
            {
                mensagem = "curtida removida com sucesso.",
                idRemovido = id
            });
        }

        [HttpGet("usuario-curtiu")]
        public async Task<IActionResult> UsuarioCurtiu([FromQuery] Guid postId, [FromQuery] Guid usuarioId)
        {
            try
            {
                var resultado = await _supabase
                    .From<Curtida>()
                    .Where(c => c.PostId == postId && c.UsuarioId == usuarioId)
                    .Get();

                bool curtiu = resultado.Models.Any();

                return Ok(new { sucesso = true, curtiu });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    sucesso = false,
                    mensagem = "Erro ao verificar curtida.",
                    detalhes = ex.Message
                });
            }
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
