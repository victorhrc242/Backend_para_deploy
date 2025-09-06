using dbRede.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace dbRede.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificacoesController : ControllerBase
    {
        private readonly IMongoCollection<Notificacao> _notificacoesCollection;

        public NotificacoesController(IConfiguration configuration)
        {
            var mongoSettings = configuration.GetSection("MongoSettings");
            var connectionString = mongoSettings.GetValue<string>("ConnectionString");
            var databaseName = mongoSettings.GetValue<string>("DatabaseName");

            var mongoClient = new MongoClient(connectionString);
            var mongoDatabase = mongoClient.GetDatabase(databaseName);
            _notificacoesCollection = mongoDatabase.GetCollection<Notificacao>("Notificacao");
        }

        // ------------------------- GET NOTIFICAÇÕES -------------------------
        [HttpGet("{usuarioId}")]
        public async Task<IActionResult> GetNotificacoes(Guid usuarioId)
        {
            try
            {
                // Busca no Mongo todas as notificações do usuário
                var filtro = Builders<Notificacao>.Filter.Eq(n => n.UsuarioId, usuarioId.ToString());
                var notificacoes = await _notificacoesCollection
                    .Find(filtro)
                    .SortByDescending(n => n.DataEnvio)
                    .ToListAsync();

                // Mapeia para DTO
                var notificacoesDto = notificacoes.Select(n => new NotificacaoDto
                {
                    Id = n.Id,
                    UsuarioId = Guid.Parse(n.UsuarioId),
                    UsuarioRemetenteId = string.IsNullOrEmpty(n.UsuarioRemetenteId) ? null : Guid.Parse(n.UsuarioRemetenteId),
                    Tipo = n.Tipo,
                    Mensagem = n.Mensagem,
                    DataEnvio = n.DataEnvio
                }).ToList();

                return Ok(new
                {
                    usuarioId,
                    total = notificacoesDto.Count,
                    notificacoes = notificacoesDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensagem = "Erro ao buscar notificações", erro = ex.Message });
            }
        }

        // ------------------------- DELETE NOTIFICAÇÃO -------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> Deletar(string id)
        {
            var filtro = Builders<Notificacao>.Filter.Eq(n => n.Id, id);
            var resultado = await _notificacoesCollection.DeleteOneAsync(filtro);

            if (resultado.DeletedCount == 0)
                return NotFound(new { erro = "Notificação não encontrada." });

            return Ok(new
            {
                mensagem = "Notificação removida com sucesso.",
                idRemovido = id
            });
        }

        // ------------------------- DTO -------------------------
        public class NotificacaoDto
        {
            public string Id { get; set; }
            public Guid UsuarioId { get; set; }
            public Guid? UsuarioRemetenteId { get; set; }
            public string Tipo { get; set; }
            public string Mensagem { get; set; }
            public DateTime DataEnvio { get; set; }
        }
    }
}
