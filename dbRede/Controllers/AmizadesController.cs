using dbRede.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;

[ApiController]
[Route("api/[controller]")]
public class AmizadesController : ControllerBase
{
    private readonly Client _supabase;

    public AmizadesController(IConfiguration configuration)
    {
        var service = new SupabaseService(configuration);
        _supabase = service.GetClient();
    }

    // Enviar solicitação
    [HttpPost("solicitar")]
    public async Task<IActionResult> EnviarSolicitacao([FromBody] SeguidorDto dto)
    {
        var seguidor = new Seguidor
        {
            Id = Guid.NewGuid(),
            Usuario1 = dto.Usuario1,
            Usuario2 = dto.Usuario2,
            Status = "pendente",
            DataSolicitacao = DateTime.UtcNow
        };

        await _supabase.From<Seguidor>().Insert(seguidor);

        // Buscar nome do Usuario1 (internamente, após inserir o Seguidor)
        var usuario1 = await _supabase.From<User>().Where(u => u.id == dto.Usuario1).Single();
        var nomeUsuario1 = usuario1?.Nome_usuario ?? "Usuário Desconhecido";

        // Buscar nome do Usuario2 (internamente, após inserir o Seguidor)
        var usuario2 = await _supabase.From<User>().Where(u => u.id == dto.Usuario2).Single();
        var nomeUsuario2 = usuario2?.Nome_usuario ?? "Usuário Desconhecido";

        // Criar notificação para o usuário2, que tem que aceitar a solicitação
        var notificacao = new Notificacao
        {
            Id = Guid.NewGuid(),
            UsuarioId = dto.Usuario2,  // Usuário que precisa ver a solicitação
            Tipo = "pendente",
            UsuarioidRemetente = dto.Usuario1,
            //Mensagem = $"{nomeUsuario1} Esta pedindo para te seguir",
            Mensagem = $"Esta pedindo para te seguir",
            DataEnvio = DateTime.UtcNow
        };

        await _supabase.From<Notificacao>().Insert(notificacao);

        return Ok(new
        {
            sucesso = true,
            mensagem = "Solicitação de seguir enviada.",
            dados = new SeguidorResponseDto(seguidor)
        });
    }
    // Solicitar e aceitar automaticamente
    [HttpPost("solicitar-e-aceitar-automaticamente")]
    public async Task<IActionResult> SolicitarEAceitarAutomaticamente([FromBody] SeguidorDto dto)
    {
        var seguidor = new Seguidor
        {
            Id = Guid.NewGuid(),
            Usuario1 = dto.Usuario1,
            Usuario2 = dto.Usuario2,
            Status = "aceito",
            DataSolicitacao = DateTime.UtcNow
        };

        await _supabase.From<Seguidor>().Insert(seguidor);

        // Buscar nome do Usuario1
        var usuario1 = await _supabase.From<User>().Where(u => u.id == dto.Usuario1).Single();
        var nomeUsuario1 = usuario1?.Nome_usuario ?? "Usuário Desconhecido";

        // Buscar nome do Usuario2
        var usuario2 = await _supabase.From<User>().Where(u => u.id == dto.Usuario2).Single();
        var nomeUsuario2 = usuario2?.Nome_usuario ?? "Usuário Desconhecido";

        // Criar notificação para o usuário2, que o seguimento já foi aceito automaticamente
        var notificacao = new Notificacao
        {
            Id = Guid.NewGuid(),
            UsuarioId = dto.Usuario2,  // Usuário que foi seguido e precisa ver a ação
            Tipo = "aceito",
            UsuarioidRemetente=dto.Usuario1,
            //Mensagem = $"{nomeUsuario1} Seguiu Voce",
            Mensagem = $"Seguiu Voce",
            DataEnvio = DateTime.UtcNow
        };

        await _supabase.From<Notificacao>().Insert(notificacao);

        return Ok(new
        {
            sucesso = true,
            mensagem = "Solicitação enviada e automaticamente aceita.",
            dados = new SeguidorResponseDto(seguidor)
        });
    }
    //     essa aceitação sera a  privada ou seja a que o usuario privada tera que fazer para segui
    //     e fazer a aceitação 
    // Aceitar solicitação
    [HttpPut("aceitar/{id}")]
    public async Task<IActionResult> AceitarSolicitacao(Guid id)
    {
        var resultado = await _supabase.From<Seguidor>().Where(s => s.Id == id).Single();

        if (resultado == null)
            return NotFound(new { sucesso = false, erro = "Solicitação não encontrada." });

        if (resultado.Status == "aceito")
            return BadRequest(new { sucesso = false, erro = "Solicitação já foi aceita." });

        resultado.Status = "aceito";
        await _supabase.From<Seguidor>().Update(resultado);

        // Buscar nome do Usuario1 e Usuario2
        var usuario1 = await _supabase.From<User>().Where(u => u.id == resultado.Usuario1).Single();
        var nomeUsuario1 = usuario1?.Nome_usuario ?? "Usuário Desconhecido"; // Se não encontrar, nome será "Usuário Desconhecido"

        var usuario2 = await _supabase.From<User>().Where(u => u.id == resultado.Usuario2).Single();
        var nomeUsuario2 = usuario2?.Nome_usuario ?? "Usuário Desconhecido";

        // Criar notificação para o usuário1 que sua solicitação foi aceita
        var notificacao = new Notificacao
        {
            Id = Guid.NewGuid(),
            UsuarioId = resultado.Usuario1,  // Usuário que enviou a solicitação
            Tipo = "aceito",
            UsuarioidRemetente=resultado.Usuario1,
           // Mensagem = $"{nomeUsuario2} aceitou sua solicitação para seguila", // Usando nome do usuario2
            Mensagem = $"aceitou sua solicitação para seguila", // Usando nome do usuario2
            DataEnvio = DateTime.UtcNow
        };

        await _supabase.From<Notificacao>().Insert(notificacao);

        return Ok(new
        {
            sucesso = true,
            mensagem = "Solicitação aceita com sucesso.",
            dados = new SeguidorResponseDto(resultado)
        });
    }
    // Recusar solicitação
    [HttpPut("recusar/{id}")]
    public async Task<IActionResult> RecusarSolicitacao(Guid id)
    {
        var resultado = await _supabase.From<Seguidor>().Where(s => s.Id == id).Single();

        if (resultado == null)
            return NotFound(new { sucesso = false, erro = "Solicitação não encontrada." });

        resultado.Status = "recusado";
        await _supabase.From<Seguidor>().Update(resultado);

        // Buscar nome do Usuario1 e Usuario2
        var usuario1 = await _supabase.From<User>().Where(u => u.id == resultado.Usuario1).Single();
        var nomeUsuario1 = usuario1?.Nome_usuario ?? "Usuário Desconhecido";

        var usuario2 = await _supabase.From<User>().Where(u => u.id == resultado.Usuario2).Single();
        var nomeUsuario2 = usuario2?.Nome_usuario ?? "Usuário Desconhecido";

        // Criar notificação para o usuário1 que sua solicitação foi recusada
        var notificacao = new Notificacao
        {
            Id = Guid.NewGuid(),
            UsuarioId = resultado.Usuario1,  // Usuário que enviou a solicitação
            Tipo = "recusado",
            UsuarioidRemetente=resultado.Usuario1,
            //Mensagem = $"{nomeUsuario2} Recusou sua solicitação para seguila",// Usando nome do usuario2
            DataEnvio = DateTime.UtcNow
        };

        await _supabase.From<Notificacao>().Insert(notificacao);

        return Ok(new
        {
            sucesso = true,
            mensagem = "Solicitação recusada com sucesso.",
            dados = new SeguidorResponseDto(resultado)
        });
    }

    //fim disso
    [HttpGet("seguindo/{usuarioId}")]
    public async Task<IActionResult> GetSeguindo(Guid usuarioId)
    {
        var resposta = await _supabase
            .From<Seguidor>()
            .Where(s => s.Usuario1 == usuarioId && s.Status == "aceito")
            .Get();

        var seguindo = resposta.Models.Select(s => new SeguidorResponseDto(s)).ToList();

        return Ok(new
        {
            sucesso = true,
            usuarioId,
            total = seguindo.Count,
            seguindo
        });
    }






    [HttpGet("seguidores/{usuarioId}")]
    public async Task<IActionResult> GetSeguidores(Guid usuarioId)
    {
        var resposta = await _supabase
            .From<Seguidor>()
            .Where(s => s.Usuario2 == usuarioId && s.Status == "aceito")
            .Get();

        var seguidores = resposta.Models.Select(s => new SeguidorResponseDto(s)).ToList();

        return Ok(new
        {
            sucesso = true,
            usuarioId,
            total = seguidores.Count,
            seguidores
        });
    }

    [HttpGet("pendentes/{usuarioId}")]
    public async Task<IActionResult> GetPendentes(Guid usuarioId)
    {
        var resposta = await _supabase
            .From<Seguidor>()
            .Where(s => s.Usuario2 == usuarioId && s.Status == "pendente")
            .Get();

        var pendentes = resposta.Models.Select(s => new
        {
            SolicitacaoId = s.Id,     // ID da solicitação para aceitar/recusar
            UsuarioRemetenteId = s.Usuario1, // Quem enviou
            UsuarioDestinoId = s.Usuario2,   // Quem recebeu (usuarioId)
            s.Status,
            s.DataSolicitacao
        }).ToList();

        return Ok(new
        {
            sucesso = true,
            usuarioId,
            total = pendentes.Count,
            pendentes
        });
    }


    [HttpGet("solicitacao/existe")]
    public async Task<IActionResult> VerificarSolicitacao([FromQuery] Guid usuario1, [FromQuery] Guid usuario2)
    {
        var resposta = await _supabase
            .From<Seguidor>()
            .Where(s => s.Usuario1 == usuario1 && s.Usuario2 == usuario2 && s.Status == "pendente")
            .Get();

        return Ok(new
        {
            sucesso = true,
            existe = resposta.Models.Any(),
            total = resposta.Models.Count
        });
    }
    [HttpDelete("deseguir")]
    public async Task<IActionResult> Deseguir([FromQuery] Guid usuario1, [FromQuery] Guid usuario2)
    {
        var resultSeguindo = await _supabase
            .From<Seguidor>()
            .Where(s => s.Usuario1 == usuario1)
            .Where(s => s.Usuario2 == usuario2)
            .Where(s => s.Status == "aceito")
            .Limit(1)
            .Get();

        var seguindo = resultSeguindo.Models.FirstOrDefault();

        var resultSendoSeguido = await _supabase
            .From<Seguidor>()
            .Where(s => s.Usuario1 == usuario2)
            .Where(s => s.Usuario2 == usuario1)
            .Where(s => s.Status == "aceito")
            .Limit(1)
            .Get();

        var sendoSeguido = resultSendoSeguido.Models.FirstOrDefault();

        bool seguiu = false;

        if (seguindo != null)
        {
            await _supabase.From<Seguidor>().Delete(seguindo);
            seguiu = true;
        }

        if (sendoSeguido != null)
        {
            await _supabase.From<Seguidor>().Delete(sendoSeguido);
            seguiu = true;
        }

        if (!seguiu)
        {
            return NotFound(new { sucesso = false, mensagem = "Nenhuma relação de seguimento encontrada entre os usuários." });
        }

        return Ok(new { sucesso = true, mensagem = "Deseguir realizado com sucesso." });
    }

    [HttpGet("segue")]
    public async Task<IActionResult> VerificaSeSegue([FromQuery] Guid usuario1, [FromQuery] Guid usuario2)
    {
        var resposta = await _supabase
            .From<Seguidor>()
            .Where(s => s.Usuario1 == usuario1)
            .Where(s => s.Usuario2 == usuario2)
            .Where(s => s.Status == "aceito")
            .Get();

        bool estaSeguindo = resposta.Models.Any();

        return Ok(new
        {
            sucesso = true,
            usuario1,
            usuario2,
            estaSeguindo
        });
    }



    public class SeguidorDto
    {
        public Guid Usuario1 { get; set; }
        public Guid Usuario2 { get; set; }
    }

    public class SeguidorResponseDto
    {
        public Guid Id { get; set; }
        public Guid Usuario1 { get; set; }
        public Guid Usuario2 { get; set; }
        public string Status { get; set; }
        public DateTime DataSolicitacao { get; set; }

        public SeguidorResponseDto() { }

        public SeguidorResponseDto(Seguidor seguidor)
        {
            Id = seguidor.Id;
            Usuario1 = seguidor.Usuario1;
            Usuario2 = seguidor.Usuario2;
            Status = seguidor.Status;
            DataSolicitacao = seguidor.DataSolicitacao;
        }
    }
}
