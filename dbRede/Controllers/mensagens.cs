using dbRede.Models;
using dbRede.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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

    public MensagensController(IConfiguration configuration, IHubContext<MensagensHub> hubContext)
    {
        var service = new SupabaseService(configuration);
        _supabase = service.GetClient();
        _hubContext = hubContext;
    }

    [HttpPost("enviar")]
    public async Task<IActionResult> Enviar([FromBody] EnviarMensagemRequest request)
    {
        var mensagem = new Mensagens
        {
            Id = Guid.NewGuid(),
            id_remetente = request.IdRemetente,
            id_destinatario = request.IdDestinatario,
            conteudo = Criptografar(request.Conteudo),
            data_envio = DateTime.UtcNow,
            lida = false,
            apagada = false
        };

        var resposta = await _supabase.From<Mensagens>().Insert(mensagem);

        if (resposta.Models.Count == 0)
            return StatusCode(500, new { sucesso = false, mensagem = "Erro ao enviar a mensagem." });

        // Notificar os clientes conectados em tempo real
        await _hubContext.Clients.User(request.IdDestinatario.ToString()).SendAsync("NovaMensagem", new
        {
            mensagem.Id,
            mensagem.id_remetente,
            mensagem.id_destinatario,
            mensagem.conteudo,
            mensagem.data_envio,
            mensagem.lida
        });

        return Ok(new
        {
            sucesso = true,
            mensagem = "Mensagem enviada com sucesso!",
            dados = new
            {
                mensagem.Id,
                mensagem.id_remetente,
                mensagem.id_destinatario,
                mensagem.conteudo,
                mensagem.data_envio,
                mensagem.lida
            }
        });
    }

    [HttpGet("mensagens/{usuario1Id}/{usuario2Id}")]
    public async Task<IActionResult> ListarMensagensEntreUsuarios(Guid usuario1Id, Guid usuario2Id)
    {
        try
        {
            var resposta1 = await _supabase
                .From<Mensagens>()
                .Filter("id_remetente", Operator.Equals, usuario1Id.ToString())
                .Filter("id_destinatario", Operator.Equals, usuario2Id.ToString())
                .Filter("apagada", Operator.Equals, "false")
                .Get();

            var resposta2 = await _supabase
                .From<Mensagens>()
                .Filter("id_remetente", Operator.Equals, usuario2Id.ToString())
                .Filter("id_destinatario", Operator.Equals, usuario1Id.ToString())
                .Filter("apagada", Operator.Equals, "false")
                .Get();

            var mensagens = resposta1.Models
                .Concat(resposta2.Models)
                .OrderBy(m => m.data_envio)
                .Select(m => new
                {
                    m.Id,
                    m.id_remetente,
                    m.id_destinatario,
                    conteudo=Descriptografar(m.conteudo), // Descriptografa o conteúdo da mensagem
                    m.data_envio,
                    m.lida,
                    m.apagada
                })
                .ToList();

            return Ok(new
            {
                sucesso = true,
                usuarios = new[] { usuario1Id, usuario2Id },
                mensagens
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                sucesso = false,
                mensagem = "Erro ao buscar mensagens.",
                erro = ex.Message
            });
        }
    }

    [HttpPut("mensagens/{mensagemId}/apagar")]
    public async Task<IActionResult> ApagarMensagem(Guid mensagemId)
    {
        var resposta = await _supabase
            .From<Mensagens>()
            .Where(m => m.Id == mensagemId)
            .Get();
        var mensagem = resposta.Models.FirstOrDefault();
        if (mensagem == null)
            return NotFound(new { sucesso = false, mensagem = "Mensagem não encontrada." });
        mensagem.apagada = true;
        await _supabase.From<Mensagens>().Update(mensagem);

        // Notificar os clientes via SignalR que a mensagem foi apagada
        await _hubContext.Clients.All.SendAsync("MensagemApagada", mensagemId);
        return Ok(new
        {
            sucesso = true,
            mensagem = "Mensagem marcada como apagada.",
            dados = new
            {
                mensagem.Id,
                mensagem.apagada
            }
        });
    }

    [HttpPut("marcar-como-lida/{mensagemId}")]
    public async Task<IActionResult> MarcarMensagemComoLida(Guid mensagemId)
    {
        var resposta = await _supabase
            .From<Mensagens>()
            .Where(m => m.Id == mensagemId)
            .Get();

        var mensagem = resposta.Models.FirstOrDefault();

        if (mensagem == null)
        {
            return NotFound(new { sucesso = false, mensagem = "Mensagem não encontrada." });
        }

        mensagem.lida = true;

        var updateResposta = await _supabase
            .From<Mensagens>()
            .Update(mensagem);
        await _hubContext.Clients.All.SendAsync("MensagemLida", mensagemId, mensagem.lida);
        return Ok(new
        {
            sucesso = true,
            mensagem = "Mensagem marcada como lida com sucesso.",
            dados = new
            {
                mensagem.Id,
                mensagem.lida
            }
        });
    }
    [HttpGet("nao-lidas/{usuarioId}")]
    public async Task<IActionResult> BuscarMensagensNaoLidas(Guid usuarioId)
    {
        try
        {
            var mensagensNaoLidas = await _supabase
     .From<Mensagens>()
     .Filter("id_destinatario", Operator.Equals, usuarioId.ToString())
     .Filter("lida", Operator.Equals, "false")
     .Filter("apagada", Operator.Equals, "false")
     .Get();
            if (mensagensNaoLidas?.Models == null)
            {
                return StatusCode(500, new { sucesso = false, erro = "Erro ao buscar mensagens: resposta nula." });
            }

            var contagem = mensagensNaoLidas.Models
                .GroupBy(m => m.id_remetente)
                .ToDictionary(g => g.Key, g => g.Count());

            return Ok(new
            {
                sucesso = true,
                naoLidas = contagem
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { sucesso = false, erro = ex.Message });
        }
    }

    //deletar mensagem
    [HttpDelete("{id}")]
    public async Task<IActionResult>deletar(Guid id)
    {
        var resultado = await _supabase.From<Mensagens>()
            .Where(n => n.Id == id)
            .Single();

        if (resultado == null)
            return NotFound(new { erro = "Mensagen Não encontrada" });
        await _supabase.From<Mensagens>().Delete(resultado);
        return Ok(new
        {
            mensagem = "Mensagem Apagada com sucesso.",
            IdRemovido = id
        });
    }
    // lista so os usuarios em que o usuario principal segue 
    [HttpGet("conversas/{usuarioId}")]
    public async Task<IActionResult> GetUsuariosComConversas(Guid usuarioId)
    {
        try
        {
            // Busca todas as mensagens onde o usuário é remetente ou destinatário
            var mensagens = await _supabase
                .From<Mensagens>()
                .Where(m => m.id_remetente == usuarioId || m.id_destinatario == usuarioId)
                .Get();

            // Coleta os IDs dos outros usuários com quem ele trocou mensagens
            var outrosUsuariosIds = mensagens.Models
                .Select(m => m.id_remetente == usuarioId ? m.id_destinatario : m.id_remetente)
                .Distinct()
                .ToList();

            var usuariosComConversa = new List<object>();

            foreach (var outroId in outrosUsuariosIds)
            {
                var userResult = await _supabase
                    .From<User>() // <-- troque se a sua entidade de usuário tiver outro nome
                    .Where(u => u.id == outroId)
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

            return Ok(new
            {
                sucesso = true,
                total = usuariosComConversa.Count,
                usuarios = usuariosComConversa
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                sucesso = false,
                mensagem = "Erro ao buscar usuários com conversas.",
                erro = ex.Message
            });
        }
    }

    public class EnviarMensagemRequest
    {
        public Guid IdRemetente { get; set; }
        public Guid IdDestinatario { get; set; }
        public string Conteudo { get; set; }
    }




    //  cripitografia usando byte  elas estão privada pois so sera usada nessa classe

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

        var base64 = Convert.ToBase64String(encrypted);
        return "[enc]" + base64;
    }


    private static string Descriptografar(string base64Criptografado)
    {
        if (!base64Criptografado.StartsWith("[enc]"))
            return base64Criptografado; // já está em texto plano, não precisa descriptografar

        base64Criptografado = base64Criptografado.Substring(5); // remove o prefixo "[enc]"

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes("1234567890abcdef1234567890abcdef");
        aes.IV = Encoding.UTF8.GetBytes("1234567890abcdef");
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var decryptor = aes.CreateDecryptor();
        var encryptedBytes = Convert.FromBase64String(base64Criptografado);
        var decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

        return Encoding.UTF8.GetString(decrypted);
    }


}
