using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Supabase;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using System.Collections.Concurrent;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using static Supabase.Postgrest.Constants;
[ApiController]
[Route("api/auth")]
public class Logi : ControllerBase
{
    private readonly Client _supabase;
    public Logi(IConfiguration configuration)
    {
        var service = new SupabaseService(configuration);
        _supabase = service.GetClient();
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var users = await _supabase.From<User>()
            .Where(u => u.Email == request.Email)
            .Get();

        if (users.Models.Count == 0)
            return Unauthorized("Usuário não encontrado");

        var user = users.Models.First();

        if (!BCrypt.Net.BCrypt.Verify(request.Senha, user.Senha))
            return Unauthorized("Senha incorreta");

        var userDTO = new UserDTO
        {
            Email = user.Email,
            id = user.id,
            Nome = user.Nome
        };

        var token = GerarToken(userDTO);

        return Ok(new
        {
            message = "Login realizado com sucesso!",
            token = token,
            user = userDTO
        });
    }
    // enviar codigo
    [HttpPost("Enviar-codigo")]
    public async Task<IActionResult> EnviarCodigo([FromBody] EnviarCodigoDTO dados)
    {
        if (string.IsNullOrEmpty(dados.Email))
            return BadRequest("Email é obrigatório.");

        var usuarioResponse = await _supabase
            .From<User>()
            .Where(u => u.Email == dados.Email)
            .Get();

        var usuario = usuarioResponse.Models.FirstOrDefault();

        if (usuario == null)
            return NotFound("Usuário não encontrado.");

        var codigo = new Random().Next(100000, 999999).ToString();

        var recovery = new PasswordRecovery
        {
            UserId = usuario.id,
            RecoveryCode = codigo,
            Expiration = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false
        };

        await _supabase.From<PasswordRecovery>().Insert(recovery);

        var emailService = new EmailService();
        await emailService.EnviarEmailAsync(
            dados.Email,
            "Recuperação de senha - Sua rede social",
            $"Olá, {usuario.Nome}! \n\nSeu código de recuperação é: {codigo}\n\nEle expira em 15 minutos.");

        return Ok("Código de recuperação enviado para o e-mail.");
    }
    // recuperar senha 

    [HttpPut("Recuperar-senha")]
    public async Task<IActionResult> RecuperarSenha([FromBody] RecuperarSenhaDTO dados)
    {
        if (string.IsNullOrWhiteSpace(dados.Email) ||
            string.IsNullOrWhiteSpace(dados.NovaSenha) ||
            string.IsNullOrWhiteSpace(dados.CodigoRecuperacao))
        {
            return BadRequest("Email, código e nova senha são obrigatórios.");
        }

        try
        {
            var usuarioResponse = await _supabase
           .From<User>()
           .Where(u => u.Email == dados.Email)
           .Get();
            if (usuarioResponse == null || usuarioResponse.Models == null || !usuarioResponse.Models.Any())
            {
                Console.WriteLine($"[DEBUG] Nenhum usuário encontrado com email: {dados.Email}");
                return NotFound("Usuário não encontrado.");
            }

            var usuario = usuarioResponse.Models.First();
            // usei o filter ao inves do were pois seria masi facil na hora de buscar
            //para editar
            var codigoResponse = await _supabase
           .From<PasswordRecovery>()
           .Filter("user_id", Operator.Equals, usuario.id.ToString())
           .Filter("recovery_code", Operator.Equals, dados.CodigoRecuperacao)
           .Filter("is_used", Operator.Equals, "false")
           .Filter("expiration", Operator.GreaterThan, DateTime.UtcNow.ToString("o"))
           .Get();

            if (codigoResponse == null || codigoResponse.Models == null || !codigoResponse.Models.Any())
            {
                return BadRequest("Código de recuperação inválido ou expirado.");
            }

            var codigo = codigoResponse.Models.First();

            // Criptografa a nova senha
            string senhaCriptografada = BCrypt.Net.BCrypt.HashPassword(dados.NovaSenha);

            // Atualiza a senha
            var updateResponse = await _supabase
                .From<User>()
                .Where(x => x.id == usuario.id)
                .Set(x => x.Senha, senhaCriptografada)
                .Update();

            if (updateResponse == null || updateResponse.Models == null || updateResponse.Models.Count == 0)
            {
                return StatusCode(500, "Erro ao atualizar a senha.");
            }

            // Marca o código como usado
            codigo.IsUsed = true;
            await _supabase.From<PasswordRecovery>().Upsert(codigo);

            return Ok("Senha atualizada com sucesso.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Erro RecuperarSenha] {ex.Message}");
            return StatusCode(500, "Ocorreu um erro ao processar a solicitação.");
        }
    }

    public class EnviarCodigoDTO
    {
        public string Email { get; set; }
    }

    public class RecuperarSenhaDTO
    {
        public string Email { get; set; }
        public string NovaSenha { get; set; }
        public string CodigoRecuperacao { get; set; } // Novo campo
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Senha { get; set; }
    }
    public class UserDTO
    {
        public string Email { get; set; }
        public Guid id { get; set; }
        public string Nome { get; set; }
    }
    private string GerarToken(UserDTO user)
    {
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("sua-chave-secreta-supersegura-aqui"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.Nome)
    };

        var token = new JwtSecurityToken(
            issuer: "suaaplicacao",
            audience: "suaaplicacao",
            claims: claims,
            expires: DateTime.Now.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}