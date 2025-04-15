using dbRede.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Supabase;
using Supabase.Postgrest.Attributes;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using static Logi;
using static Supabase.Postgrest.Constants;

[ApiController]
[Route("api/auth")]
public class RegisterController : ControllerBase
{
    private readonly Client _supabase;

    public RegisterController(IConfiguration configuration)
    {
        var service = new SupabaseService(configuration);
        _supabase = service.GetClient();
    }
    //aqui estava dando erro pois  o status body não estava fucionando pois não
    //estava conseguindo retornar  um json valido ele tentava retornar os dados de
    // um jeito diferente do que era para retornar pois precisava retornar um formato json completo 
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromForm] RegisterRequest request, IFormFile fotoPerfil)
    {
        if (string.IsNullOrWhiteSpace(request.Nome) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Senha) ||
            string.IsNullOrWhiteSpace(request.biografia) ||
            string.IsNullOrWhiteSpace(request.dataaniversario) ||
            fotoPerfil == null)
        {
            return BadRequest(new { error = "Todos os campos são obrigatórios." });
        }

        try
        {
            var nomeNormalizado = request.Nome.Trim().ToLower();
            var emailNormalizado = request.Email.Trim().ToLower();

            var existingEmail = await _supabase.From<User>().Where(u => u.Email == emailNormalizado).Get();
            if (existingEmail.Models.Any())
                return BadRequest(new { error = "E-mail já cadastrado." });

            var existingNome = await _supabase.From<User>().Where(u => u.Nome == nomeNormalizado).Get();
            if (existingNome.Models.Any())
                return BadRequest(new { error = "Nome de usuário já está em uso." });

            // Salvar imagem no disco ou serviço
            string fileName = $"{Guid.NewGuid()}_{fotoPerfil.FileName}";
            string filePath = Path.Combine("wwwroot/uploads", fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fotoPerfil.CopyToAsync(stream);
            }

            string urlDaImagem = $"/uploads/{fileName}";

            var senhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha);

            var newUser = new User
            {
                Nome = nomeNormalizado,
                Email = emailNormalizado,
                Senha = senhaHash,
                FotoPerfil = urlDaImagem,
                biografia = request.biografia,
                dataaniversario = request.dataaniversario
            };

            await _supabase.From<User>().Insert(newUser);

            return Ok(new
            {
                message = "Usuário cadastrado com sucesso!",
                user = new
                {
                    newUser.Nome,
                    newUser.Email,
                    newUser.FotoPerfil,
                    newUser.biografia,
                    newUser.dataaniversario
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Erro interno no servidor.", details = ex.Message });
        }
    }


    [HttpGet("usuario")]
    public async Task<IActionResult> ListarUsuarios()
    {
        var usuariosRelacionados = await _supabase
            .From<User>()
        .Get();

        var usuarios = usuariosRelacionados.Models.Select(u => new UserDto
        {
            Id = u.id,
            Nome = u.Nome,
            Email = u.Email,
            imagem=u.FotoPerfil
            
        });

        return Ok(usuarios);
    }
    [HttpGet("usuario/{id}")]
    public async Task<IActionResult> ObterUsuarioPorId(Guid id)
    {
        var resultado = await _supabase
            .From<User>()
            .Where(u => u.id == id)
            .Get();

        var usuario = resultado.Models.FirstOrDefault();

        if (usuario == null)
            return NotFound(new { erro = "Usuário não encontrado" });

        var usuarioDto = new UserDto
        {
            Id = usuario.id,
            Nome = usuario.Nome,
            Email = usuario.Email,
           imagem=usuario.FotoPerfil
            
        };

        return Ok(usuarioDto);
    }
    [HttpGet("buscar-por-nome/{nome}")]
    public async Task<IActionResult> ObterUsuarioPorNome(string nome)
    {
        var resultado = await _supabase
            .From<User>()
            .Where(u => u.Nome == nome)
            .Get();

        var usuario = resultado.Models.FirstOrDefault();

        if (usuario == null)
            return NotFound(new { erro = "Usuário não encontrado" });

        var usuarioDto = new UserDto
        {
            Id = usuario.id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            imagem= usuario.FotoPerfil
        };

        return Ok(usuarioDto);
    }
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string imagem { get; set; }
    }


    // Classe para receber os dados do cadastro
    public class RegisterRequest
    {

        public string Nome { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
        public IFormFile FotoPerfil { get; set; }
        public string biografia { get; set; }
        public string dataaniversario { get; set; }
    }
}
