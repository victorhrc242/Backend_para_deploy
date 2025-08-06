using dbRede.Models;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Utilities.Collections;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace dbRede.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoriesController : ControllerBase
    {
        private readonly Client _supabase;

        public StoriesController(IConfiguration configuration)
        {
            var service = new SupabaseService(configuration);
            _supabase = service.GetClient();
        }

        public class CriarStoryRequest
        {
            public Guid UsuarioId { get; set; }
            public string ConteudoUrl { get; set; }
            public string Tipo { get; set; } // "imagem" ou "video"
        }
        // criar storys
        [HttpPost("criar")]
        public async Task<IActionResult> CriarStory([FromBody] CriarStoryRequest novoStory)
        {
            if (string.IsNullOrEmpty(novoStory.ConteudoUrl))
            {
                return BadRequest(new { erro = "Conteúdo (imagem ou vídeo) obrigatório." });
            }

            if (novoStory.Tipo != "imagem" && novoStory.Tipo != "video")
            {
                return BadRequest(new { erro = "Tipo deve ser 'imagem' ou 'video'." });
            }

            var story = new stories
            {
                id = Guid.NewGuid(),
                id_usuario = novoStory.UsuarioId,
                conteudo_url = novoStory.ConteudoUrl,
                tipo = novoStory.Tipo,
                data_criacao = DateTime.UtcNow,
                data_expiracao = DateTime.UtcNow.AddHours(24),
                visualizacoes = 0,
                ativo = true
            };

            var resposta = await _supabase.From<stories>().Insert(story);
            var storySalvo = resposta.Models.FirstOrDefault();

            if (storySalvo == null)
                return StatusCode(500, new { erro = "Erro ao salvar o story." });

            var resultado = await _supabase
                .From<stories>()
                .Select("*, users (nome)")
                .Filter("id", Operator.Equals, storySalvo.id.ToString())
                .Get();

            var storyComUsuario = resultado.Models.FirstOrDefault();

            var dto = new StoryDto
            {
                Id = storyComUsuario.id,
                UsuarioId = storyComUsuario.id_usuario,
                ConteudoUrl = storyComUsuario.conteudo_url,
                Tipo = storyComUsuario.tipo,
                DataCriacao = storyComUsuario.data_criacao,
                DataExpiracao = storyComUsuario.data_expiracao,
                visualizacaoes = storyComUsuario.visualizacoes,
                Ativo = storyComUsuario.ativo,
            };


            return Ok(new
            {
                mensagem = "Story criado com sucesso!",
                story = dto
            });
        }
        //listar todos os storys
        [HttpGet("todos")]
        public async Task<IActionResult> ListarTodosStories()
        {
            var resultado = await _supabase
                .From<stories>()
                .Select("*, users (nome, foto_perfil)")
                .Order("data_criacao", Ordering.Descending)
                .Get();

            var listaStories = resultado.Models.Select(s => new
            {
                Id = s.id,
                UsuarioId = s.id_usuario,
                ConteudoUrl = s.conteudo_url,
                Tipo = s.tipo,
                DataCriacao = s.data_criacao,
                DataExpiracao = s.data_expiracao,
                Visualizacoes = s.visualizacoes,
                Ativo = s.ativo,
            });

            return Ok(listaStories);
        }
        [HttpGet("feed-usuarioseguindos/{usuarioId}/stories")]
        public async Task<IActionResult> ListarStoriesDosSeguidos(Guid usuarioId)
        {
            // Buscar os IDs dos usuários que o usuário segue com status "aceito"
            var amigos = await _supabase
                .From<Seguidor>()
                .Filter("usuario1", Operator.Equals, usuarioId.ToString())
                .Filter("status", Operator.Equals, "aceito")
                .Get();

            var idsSeguidos = amigos.Models.Select(a => a.Usuario2).ToList();

            // Inclui o próprio usuário (opcional)
            if (!idsSeguidos.Contains(usuarioId))
                idsSeguidos.Add(usuarioId);

            // Buscar todos os stories ativos e não expirados
            var todosStories = await _supabase
                .From<stories>()
                .Filter("ativo", Operator.Equals, "true")
                .Filter("data_expiracao", Operator.GreaterThan, DateTime.UtcNow.ToString("o"))
                .Get();

            // Filtrar apenas os stories dos usuários seguidos
            var storiesSeguidos = todosStories.Models
                .Where(s => idsSeguidos.Contains(s.id_usuario))
                .Select(s => new StoryDto
                {
                    Id = s.id,
                    UsuarioId = s.id_usuario,
                    ConteudoUrl = s.conteudo_url,
                    DataCriacao = s.data_criacao,
                    DataExpiracao = s.data_expiracao,
                    Tipo=s.tipo
                })
                .ToList();

            return Ok(storiesSeguidos);
        }

        [HttpGet("usuario-listar-stories-de-todo-mundo/{usuarioIdAlvo}/visualizar/{usuarioPrincipalId}")]
        public async Task<IActionResult> ListarStoriesDeUsuario(Guid usuarioIdAlvo, Guid usuarioPrincipalId)
        {
            // 1. Buscar o usuário alvo
            var usuarioResult = await _supabase
               .From<User>()
               .Filter("id", Operator.Equals, usuarioIdAlvo.ToString())
               .Single();

            var usuario = usuarioResult;
            if (usuario == null)
                return NotFound("Usuário não encontrado.");

            // 2. Verificar se é uma conta privada
            bool contaPrivada = !usuario.publica;

            // 3. Se for privada, verificar se o usuárioPrincipal segue essa conta
            if (contaPrivada)
            {
                var amizadeResponse = await _supabase
                    .From<Seguidor>()
                    .Filter("usuario1", Operator.Equals, usuarioPrincipalId.ToString())
                    .Filter("usuario2", Operator.Equals, usuarioIdAlvo.ToString())
                    .Filter("status", Operator.Equals, "aceito")
                    .Get();

                if (!amizadeResponse.Models.Any())
                {
                    // Conta privada e usuário não segue -> bloqueia o acesso
                    return Ok(new List<object>());
                }
            }

            // 4. Buscar stories ativos e não expirados do usuário
            var storiesResponse = await _supabase
                .From<stories>()
                .Filter("usuario_id", Operator.Equals, usuarioIdAlvo.ToString())
                .Filter("ativo", Operator.Equals, "true")
                .Filter("data_expiracao", Operator.GreaterThan, DateTime.UtcNow.ToString("o"))
                .Order("data_criacao", Ordering.Descending)
                .Get();

            var stories = storiesResponse.Models.Select(s => new
            {
                Id = s.id,
                UsuarioId = s.id_usuario,
                ConteudoUrl = s.conteudo_url,
                Tipo = s.tipo,
                DataCriacao = s.data_criacao,
                DataExpiracao = s.data_expiracao,
                Visualizacoes = s.visualizacoes,
                Ativo = s.ativo
            });

            return Ok(stories);
        }


        // registrar visualização
        [HttpPost("story/{storyId}/visualizacao")]
        public async Task<IActionResult> RegistrarVisualizacaoStory(Guid storyId, [FromQuery] Guid usuarioId, [FromQuery] int tempoEmSegundos = 0)
        {
            if (tempoEmSegundos < 0.5)
            {
                return BadRequest(new { erro = "Tempo de visualização insuficiente." });
            }

            // Busca visualizações anteriores do usuário para esse story
            var visualizacoes = await _supabase
                .From<VisualizacaoStory>()
                .Where(v => v.usuario_id == usuarioId && v.story_id == storyId)
                .Get();

            // Validação: já visualizou nos últimos 5 minutos?
            if (visualizacoes.Models.Any())
            {
                var ultima = visualizacoes.Models.OrderByDescending(v => v.data_visualizacao).First();
                var tempo = DateTime.UtcNow - ultima.data_visualizacao;
                if (tempo.TotalMinutes < 5)
                {
                    return Ok(new { mensagem = "Visualização já registrada recentemente." });
                }
            }

            // Salva nova visualização
            await _supabase.From<VisualizacaoStory>().Insert(new VisualizacaoStory
            {
                id = Guid.NewGuid(),
                usuario_id = usuarioId,
                story_id = storyId,
                data_visualizacao = DateTime.UtcNow,
                tempo_em_segundos = tempoEmSegundos
            });

            // Atualiza contagem de visualizações na tabela stories
            var storyResp = await _supabase.From<stories>().Where(s => s.id == storyId).Get();
            if (!storyResp.Models.Any())
            {
                return NotFound(new { erro = "Story não encontrado." });
            }

            var story = storyResp.Models[0];
            story.visualizacoes = story.visualizacoes += 1;

            var update = await _supabase
                .From<stories>()
                .Where(s => s.id == storyId)
                .Update(story);

            if (!update.Models.Any())
            {
                return StatusCode(500, new { erro = "Erro ao atualizar story." });
            }

            return Ok(new { mensagem = "Visualização de story registrada com sucesso." });
        }
        // listar os usuarios que visualizaram o story
        [HttpGet("story/{storyId}/visualizadores")]
        public async Task<IActionResult> ListarIdsUsuariosQueViramStory(Guid storyId)
        {
            var visualizacoes = await _supabase
                .From<VisualizacaoStory>()
                .Select("usuario_id")
                .Where(v => v.story_id == storyId)
                .Get();

            if (!visualizacoes.Models.Any())
                return NotFound(new { mensagem = "Nenhuma visualização encontrada para esse story." });

            var idsUsuarios = visualizacoes.Models.Select(v => v.usuario_id).Distinct().ToList();

            return Ok(idsUsuarios);
        }

        public class StoryDto
        {
            public Guid Id { get; set; }
            public Guid UsuarioId { get; set; }
            public string ConteudoUrl { get; set; }
            public string Tipo { get; set; }
            public DateTime DataCriacao { get; set; }
            public DateTime DataExpiracao { get; set; }
            public int visualizacaoes { get; set; }
            public bool Ativo { get; set; }
        }

    }
}
