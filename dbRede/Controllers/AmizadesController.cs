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

        return Ok(new
        {
            sucesso = true,
            mensagem = "Solicitação de seguir enviada.",
            dados = new SeguidorResponseDto(seguidor)
        });
    }

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

        var notificacao = new Notificacao
        {
            Id = Guid.NewGuid(),
            UsuarioId = resultado.Usuario1,
            Tipo = "seguindo", // Confirme se esse valor está permitido no Supabase
            ReferenciaId = resultado.Id,
            Mensagem = "Sua solicitação de seguir foi aceita!",
            DataEnvio = DateTime.UtcNow
        };

        await _supabase.From<Notificacao>().Insert(notificacao);

        return Ok(new
        {
            sucesso = true,
            mensagem = "Solicitação aceita com sucesso.",
            dados = new
            {
                solicitacao = new SeguidorResponseDto(resultado),
                notificacao
            }
        });
    }

    [HttpPut("recusar/{id}")]
    public async Task<IActionResult> RecusarSolicitacao(Guid id)
    {
        var resultado = await _supabase.From<Seguidor>().Where(s => s.Id == id).Single();

        if (resultado == null)
            return NotFound(new { sucesso = false, erro = "Solicitação não encontrada." });

        resultado.Status = "recusado";
        await _supabase.From<Seguidor>().Update(resultado);

        return Ok(new
        {
            sucesso = true,
            mensagem = "Solicitação recusada com sucesso.",
            dados = new SeguidorResponseDto(resultado)
        });
    }

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

        var pendentes = resposta.Models.Select(s => new SeguidorResponseDto(s)).ToList();

        return Ok(new
        {
            sucesso = true,
            usuarioId,
            total = pendentes.Count
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
