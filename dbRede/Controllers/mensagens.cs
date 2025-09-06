using dbRede.Models;
using dbRede.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Supabase;
using System.Security.Cryptography;
using System.Text;
using static Supabase.Postgrest.Constants;

[ApiController]
[Route("api/[controller]")]
public class MensagensController : ControllerBase
{
    private readonly Client _supabase;
    private readonly IHubContext<MensagensHub> _hubContext;
    private readonly IMongoCollection<MensagemMongo> _mensagensCollection;

    public MensagensController(IConfiguration configuration, IHubContext<MensagensHub> hubContext)
    {
        // Supabase para usuários
        var service = new SupabaseService(configuration);
        _supabase = service.GetClient();

        // SignalR
        _hubContext = hubContext;

        // MongoDB para mensagens
        var mongoSettings = configuration.GetSection("MongoSettings");
        var connectionString = mongoSettings.GetValue<string>("ConnectionString");
        var databaseName = mongoSettings.GetValue<string>("DatabaseName");

        var mongoClient = new MongoClient(connectionString);
        var mongoDatabase = mongoClient.GetDatabase(databaseName);
        _mensagensCollection = mongoDatabase.GetCollection<MensagemMongo>("mensagens");
    }

    [HttpPost("enviar")]
    public async Task<IActionResult> Enviar([FromBody] EnviarMensagemRequest request)
    {
        var mensagem = new MensagemMongo
        {
            Id = Guid.NewGuid().ToString(),
            IdRemetente = request.IdRemetente.ToString(),
            IdDestinatario = request.IdDestinatario.ToString(),
            Conteudo = Criptografar(request.Conteudo),
            DataEnvio = DateTime.UtcNow,
            Lida = false,
            Apagada = false
        };

        await _mensagensCollection.InsertOneAsync(mensagem);

        var mensagemDto = new
        {
            mensagem.Id,
            id_remetente = mensagem.IdRemetente,
            id_destinatario = mensagem.IdDestinatario,
            conteudo = Descriptografar(mensagem.Conteudo),
            data_envio = mensagem.DataEnvio,
            lida = mensagem.Lida,
            apagada = mensagem.Apagada
        };

        // notificação em tempo real
        await _hubContext.Clients.User(request.IdDestinatario.ToString())
            .SendAsync("NovaMensagem", mensagemDto);

        // retorno da API
        return Ok(new { sucesso = true, dados = mensagemDto });
    }


   // ------------------------- ENVIAR COM POST -------------------------
[HttpPost("enviar-com-post")]
public async Task<IActionResult> EnviarMensagemComPost([FromBody] EnviarMensagemComPostRequest request)
{
    var mensagem = new MensagemMongo
    {
        Id = Guid.NewGuid().ToString(),
        IdRemetente = request.IdRemetente.ToString(),
        IdDestinatario = request.IdDestinatario.ToString(),
        Conteudo = Criptografar(request.Conteudo ?? ""),
        DataEnvio = DateTime.UtcNow,
        Lida = false,
        Apagada = false,
        PostCompartilhadoId = request.PostCompartilhadoId?.ToString()
    };

    await _mensagensCollection.InsertOneAsync(mensagem);

    var mensagemDto = new
    {
        mensagem.Id,
        mensagem.IdRemetente,
        mensagem.IdDestinatario,
        Conteudo = Descriptografar(mensagem.Conteudo),
        mensagem.DataEnvio,
        mensagem.Lida,
        mensagem.PostCompartilhadoId
    };

    await _hubContext.Clients.User(request.IdDestinatario.ToString())
        .SendAsync("NovaMensagem", mensagemDto);

    return Ok(new { sucesso = true, dados = mensagemDto });
}

// ------------------------- ENVIAR COM STORY -------------------------
[HttpPost("enviar-com-story")]
public async Task<IActionResult> EnviarMensagemComStory([FromBody] EnviarMensagemComStoryRequest request)
{
    var mensagem = new MensagemMongo
    {
        Id = Guid.NewGuid().ToString(),
        IdRemetente = request.IdRemetente.ToString(),
        IdDestinatario = request.IdDestinatario.ToString(),
        Conteudo = Criptografar(request.Conteudo),
        DataEnvio = DateTime.UtcNow,
        Lida = false,
        Apagada = false,
        StoryId = request.StoryId?.ToString()
    };

    await _mensagensCollection.InsertOneAsync(mensagem);

    var mensagemDto = new
    {
        mensagem.Id,
        mensagem.IdRemetente,
        mensagem.IdDestinatario,
        Conteudo = Descriptografar(mensagem.Conteudo),
        mensagem.DataEnvio,
        mensagem.Lida,
        mensagem.StoryId
    };

    await _hubContext.Clients.User(request.IdDestinatario.ToString())
        .SendAsync("NovaMensagem", mensagemDto);

    return Ok(new { sucesso = true, dados = mensagemDto });
}

    // ------------------------- LISTAR MENSAGENS ENTRE USUÁRIOS -------------------------
    [HttpGet("mensagens/{usuario1Id}/{usuario2Id}")]
    public async Task<IActionResult> ListarMensagensEntreUsuarios(Guid usuario1Id, Guid usuario2Id)
    {
        var filtro = Builders<MensagemMongo>.Filter.Or(
            Builders<MensagemMongo>.Filter.And(
                Builders<MensagemMongo>.Filter.Eq(m => m.IdRemetente, usuario1Id.ToString()),
                Builders<MensagemMongo>.Filter.Eq(m => m.IdDestinatario, usuario2Id.ToString()),
                Builders<MensagemMongo>.Filter.Eq(m => m.Apagada, false)
            ),
            Builders<MensagemMongo>.Filter.And(
                Builders<MensagemMongo>.Filter.Eq(m => m.IdRemetente, usuario2Id.ToString()),
                Builders<MensagemMongo>.Filter.Eq(m => m.IdDestinatario, usuario1Id.ToString()),
                Builders<MensagemMongo>.Filter.Eq(m => m.Apagada, false)
            )
        );

        var mensagensDb = await _mensagensCollection
            .Find(filtro)
            .SortBy(m => m.DataEnvio)
            .ToListAsync();

        var mensagens = mensagensDb
            .Select(m => new
            {
                m.Id,
                id_remetente = m.IdRemetente,
                id_destinatario = m.IdDestinatario,
                conteudo = Descriptografar(m.Conteudo),
                data_envio = m.DataEnvio,
                lida = m.Lida,
                apagada = m.Apagada,
                Postid = m.PostCompartilhadoId
            })
            .ToList();

        return Ok(new
        {
            sucesso = true,
            usuarios = new[] { usuario1Id, usuario2Id },
            mensagens
        });
    }

    // ------------------------- APAGAR -------------------------
    [HttpPut("mensagens/{mensagemId}/apagar")]
    public async Task<IActionResult> ApagarMensagem(string mensagemId)
    {
        var filtro = Builders<MensagemMongo>.Filter.Eq(m => m.Id, mensagemId);
        var update = Builders<MensagemMongo>.Update.Set(m => m.Apagada, true);

        var result = await _mensagensCollection.UpdateOneAsync(filtro, update);

        if (result.ModifiedCount == 0)
            return NotFound(new { sucesso = false, mensagem = "Mensagem não encontrada." });

        await _hubContext.Clients.All.SendAsync("MensagemApagada", mensagemId);

        return Ok(new { sucesso = true, mensagem = "Mensagem marcada como apagada." });
    }

    // ------------------------- MARCAR COMO LIDA -------------------------
    [HttpPut("marcar-como-lida/{mensagemId}")]
    public async Task<IActionResult> MarcarMensagemComoLida(string mensagemId)
    {
        var filtro = Builders<MensagemMongo>.Filter.Eq(m => m.Id, mensagemId);
        var update = Builders<MensagemMongo>.Update.Set(m => m.Lida, true);

        var result = await _mensagensCollection.UpdateOneAsync(filtro, update);

        if (result.ModifiedCount == 0)
            return NotFound(new { sucesso = false, mensagem = "Mensagem não encontrada." });

        await _hubContext.Clients.All.SendAsync("MensagemLida", mensagemId, true);

        return Ok(new { sucesso = true, mensagem = "Mensagem marcada como lida." });
    }

    // ------------------------- BUSCAR NÃO LIDAS -------------------------
    [HttpGet("nao-lidas/{usuarioId}")]
    public async Task<IActionResult> BuscarMensagensNaoLidas(Guid usuarioId)
    {
        var filtro = Builders<MensagemMongo>.Filter.And(
            Builders<MensagemMongo>.Filter.Eq(m => m.IdDestinatario, usuarioId.ToString()),
            Builders<MensagemMongo>.Filter.Eq(m => m.Lida, false),
            Builders<MensagemMongo>.Filter.Eq(m => m.Apagada, false)
        );

        var mensagens = await _mensagensCollection.Find(filtro).ToListAsync();

        var contagem = mensagens.GroupBy(m => m.IdRemetente)
                                .ToDictionary(g => g.Key, g => g.Count());

        return Ok(new { sucesso = true, naoLidas = contagem });
    }
    // ------------------------- GET USUÁRIOS COM CONVERSAS -------------------------
    [HttpGet("conversas/{usuarioId}")]
    public async Task<IActionResult> GetUsuariosComConversas(Guid usuarioId)
    {
        // Pega todos os chats do Mongo
        var filtro = Builders<MensagemMongo>.Filter.Or(
            Builders<MensagemMongo>.Filter.Eq(m => m.IdRemetente, usuarioId.ToString()),
            Builders<MensagemMongo>.Filter.Eq(m => m.IdDestinatario, usuarioId.ToString())
        );

        var mensagens = await _mensagensCollection.Find(filtro).ToListAsync();

        var outrosUsuariosIds = mensagens
            .Select(m => m.IdRemetente == usuarioId.ToString() ? m.IdDestinatario : m.IdRemetente)
            .Distinct()
            .ToList();

        var usuariosComConversa = new List<object>();

        foreach (var outroId in outrosUsuariosIds)
        {
            var userResult = await _supabase
                .From<User>()
                .Filter("id", Operator.Equals, outroId)
                .Get();

            var user = userResult.Models.FirstOrDefault();
            if (user != null)
            {
                usuariosComConversa.Add(new
                {
                    user.id,
                    user.Nome_usuario,
                    user.FotoPerfil
                });
            }
        }

        return Ok(new { sucesso = true, usuarios = usuariosComConversa });
    }

    // ------------------------- MODELOS -------------------------
    public class MensagemMongo
    {
        [BsonId] public string Id { get; set; }
        public string IdRemetente { get; set; }
        public string IdDestinatario { get; set; }
        public string Conteudo { get; set; }
        public bool Lida { get; set; }
        public bool Apagada { get; set; }
        public DateTime DataEnvio { get; set; }
        public string PostCompartilhadoId { get; set; }
        public string StoryId { get; set; }
    }

    public class EnviarMensagemRequest
    {
        public Guid IdRemetente { get; set; }
        public Guid IdDestinatario { get; set; }
        public string Conteudo { get; set; }
    }

    public class EnviarMensagemComPostRequest
    {
        public Guid IdRemetente { get; set; }
        public Guid IdDestinatario { get; set; }
        public string Conteudo { get; set; }
        public Guid? PostCompartilhadoId { get; set; }
    }

    public class EnviarMensagemComStoryRequest
    {
        public Guid IdRemetente { get; set; }
        public Guid IdDestinatario { get; set; }
        public string Conteudo { get; set; }
        public Guid? StoryId { get; set; }
    }

    // ------------------------- CRIPTOGRAFIA -------------------------
    private static string Criptografar(string texto)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes("1234567890abcdef1234567890abcdef");
        aes.IV = Encoding.UTF8.GetBytes("1234567890abcdef");
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var encryptor = aes.CreateEncryptor();
        var inputBytes = Encoding.UTF8.GetBytes(texto);
        var encrypted = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

        return "[enc]" + Convert.ToBase64String(encrypted);
    }

    private static string Descriptografar(string base64Criptografado)
    {
        if (!base64Criptografado.StartsWith("[enc]")) return base64Criptografado;

        base64Criptografado = base64Criptografado.Substring(5);

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes("1234567890abcdef1234567890abcdef");
        aes.IV = Encoding.UTF8.GetBytes("1234567890abcdef");
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var encryptedBytes = Convert.FromBase64String(base64Criptografado);
        var decrypted = aes.CreateDecryptor().TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

        return Encoding.UTF8.GetString(decrypted);
    }
}
