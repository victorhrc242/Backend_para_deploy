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
    //  chama o cliente do supabase
    private readonly Client _supabase;

    public RegisterController(IConfiguration configuration)
    {
        var service = new SupabaseService(configuration);
        _supabase = service.GetClient();
    }
    //aqui estava dando erro pois  o status body não estava fucionando pois não
    //estava conseguindo retornar  um json valido ele tentava retornar os dados de
    // um jeito diferente do que era para retornar pois precisava retornar um formato json completo 

    //   registrar usuarios
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome) ||
            string.IsNullOrWhiteSpace(request.Nome_usuario) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Senha) ||
            string.IsNullOrWhiteSpace(request.biografia) ||
            string.IsNullOrWhiteSpace(request.dataaniversario) ||
            string.IsNullOrWhiteSpace(request.FotoPerfil))
        {
            return BadRequest(new { error = "Todos os campos são obrigatórios." });
        }

        try
        {
            var nomeusuarioNomralizado = request.Nome_usuario.Trim().ToLower();
            var nomeNormalizado = request.Nome.Trim().ToLower();
            var emailNormalizado = request.Email.Trim().ToLower();

            // Verificar se o email já está cadastrado
            var existingEmail = await _supabase.From<User>().Where(u => u.Email == emailNormalizado).Get();
            if (existingEmail.Models.Any())
                return BadRequest(new { error = "E-mail já cadastrado." });

            // Verificar se o nome de usuário já está em uso
            var existingNome = await _supabase.From<User>().Where(u => u.Nome_usuario == nomeNormalizado).Get();
            if (existingNome.Models.Any())
                return BadRequest(new { error = "Nome de usuário já está em uso." });

            // Hash da senha
            var senhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha);

            var newUser = new User
            {
                Nome = nomeNormalizado,
                Nome_usuario = nomeusuarioNomralizado,
                Email = emailNormalizado,
                Senha = senhaHash,
                FotoPerfil = request.FotoPerfil, // A URL da foto de perfil
                biografia = request.biografia,
                dataaniversario = request.dataaniversario
            };

            // Inserir o novo usuário no banco
            await _supabase.From<User>().Insert(newUser);

            return Ok(new
            {
                message = "Usuário cadastrado com sucesso!",
                user = new
                {
                    newUser.Nome,
                    newUser.Nome_usuario,
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

    // edita algumas infomraçãoes do usuario
    [HttpPut("editarusuarios/{id}")]
    public async Task<IActionResult> EditarUsuario(Guid id, [FromBody] putuser dados)
    {
        try
        {
            var usuario = await _supabase.From<User>().Where(u => u.id == id).Single();
            if (usuario == null) return NotFound("Usuário não encontrado");

            usuario.Nome_usuario = dados.Nome;
            usuario.biografia = dados.biografia;
            usuario.FotoPerfil = dados.imagem; // Já é uma URL

            var resultado = await _supabase
      .From<User>()
      .Where(u => u.id == id)
      .Set(u => u.Nome_usuario, usuario.Nome_usuario)
      .Set(u => u.biografia, usuario.biografia)
      .Set(u => u.FotoPerfil, usuario.FotoPerfil)
      .Update();

            var usuariosDto = resultado.Models.Select(u => new UserDto
            {
                Id = u.id,
                Nome_usuario = u.Nome_usuario,
                imagem = u.FotoPerfil,
                biografia = u.biografia
            });

            return Ok(usuariosDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erro ao editar usuário: {ex.Message}");
        }
    }



    //  da um select completo en todos os usuarios
    [HttpGet("usuario")]
    public async Task<IActionResult> ListarUsuarios()
    {
        var usuariosRelacionados = await _supabase
            .From<User>()
        .Get();

        var usuarios = usuariosRelacionados.Models.Select(u => new UserDto
        {
            Id = u.id,
            Nome_usuario = u.Nome_usuario,
            publico = u.publica
        });

        return Ok(usuarios);
    }



    // buscar infomraçãoes do usuario por id
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
            Nome_usuario = usuario.Nome_usuario,
            Nome = usuario.Nome,
            biografia = usuario.biografia,
            Email = usuario.Email,
            imagem = usuario.FotoPerfil,
            dataaniversario = usuario.dataaniversario,
            publico = usuario.publica
        };
        return Ok(usuarioDto);
    }


    // buscar usuarios por nome
    [HttpGet("buscar-por-nome/{nome}")]
    public async Task<IActionResult> ObterUsuariosPorNome(string nome)
    {
        var resultado = await _supabase
            .From<User>()
            .Filter("Nome-usuario", Operator.ILike, $"%{nome}%") // corrigido aqui
            .Get();

        if (!resultado.ResponseMessage.IsSuccessStatusCode)
        {
            return StatusCode((int)resultado.ResponseMessage.StatusCode, new { erro = "Erro na consulta ao banco" });
        }

        var modelos = resultado.Models;
        if (modelos == null || modelos.Count == 0)
            return NotFound(new { erro = "Nenhum usuário encontrado" });

        var dto = modelos.Select(u => new UserDto
        {
            Id = u.id,
            Nome_usuario = u.Nome_usuario,
            imagem = u.FotoPerfil,
            publico =u.publica
        }).ToList();

        return Ok(dto);
    }

    [HttpPut("alterar-status/{id}")]
    public async Task<IActionResult> AlterarStatusConta(Guid id)
    {
        try
        {
            // Buscar o usuário pelo ID
            var usuario = await _supabase.From<User>().Where(u => u.id == id).Single();

            if (usuario == null)
            {
                return NotFound(new { erro = "Usuário não encontrado" });
            }

            // Alternar o status de 'publica' (de verdadeiro para falso, ou vice-versa)
            usuario.publica = !usuario.publica;

            // Atualizar o usuário com o novo status
            var resultado = await _supabase
                .From<User>()
                .Where(u => u.id == id)
                .Set(u => u.publica, usuario.publica)
                .Update();

            // Se a atualização for bem-sucedida, retornar a nova configuração
            return Ok(new
            {
                message = "Status da conta alterado com sucesso!",
                usuario = new
                {
                    usuario.id,
                    usuario.Nome_usuario,
                    usuario.publica
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { erro = "Erro interno no servidor", details = ex.Message });
        }
    }
    // comentado para caso se necessario usar 
    //// Verificar se a conta do usuário é privada  
    //[HttpGet("verificar-status/{id}")]
    //public async Task<IActionResult> VerificarStatusConta(Guid id)
    //{
    //    try
    //    {
    //        // Buscar o usuário pelo ID
    //        var usuario = await _supabase.From<User>().Where(u => u.id == id).Single();

    //        if (usuario == null)
    //        {
    //            return NotFound(new { erro = "Usuário não encontrado" });
    //        }

    //        // Retorna o status da conta (se é pública ou privada)
    //        return Ok(new
    //        {
    //            message = "Status da conta recuperado com sucesso!",
    //            status = usuario.publica ? "Pública" : "Privada",
    //            usuarioId = usuario.id
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        return StatusCode(500, new { erro = "Erro interno no servidor", details = ex.Message });
    //    }
    //}
    //   dtos
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string imagem { get; set; }
        public string biografia { get; set; }
        public string Nome_usuario { get; set; }
        public string dataaniversario { get; set; }
        public bool publico { get; set; }
    }
    public class putuser
    {
        public string? Nome { get; set; }
        public string? imagem { get; set; }
        public string? biografia { get; set; }
    }

    // Classe para receber os dados do cadastro
    public class RegisterRequest
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
        public string FotoPerfil { get; set; }
        public string biografia { get; set; }
        public string dataaniversario { get; set; }
        public string Nome_usuario { get; set; }
    }
}
