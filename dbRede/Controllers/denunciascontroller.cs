using dbRede.Hubs;
using dbRede.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Supabase;
using System.ComponentModel;
using static dbRede.Controllers.FeedController;

namespace dbRede.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class denunciascontroller : ControllerBase
    {
        // construtor
        private readonly Client _supabase;
        public denunciascontroller(IConfiguration configuration)
        {
            var service = new SupabaseService(configuration);
            _supabase = service.GetClient();
        }

     
        //inserte
        [HttpPost("adicionar_denuncia")]
        public async Task<IActionResult> fazer_denuncia([FromBody] CriarDenuncia criarDenuncia)
        {
            // valores para passar os valores para o banco
            var denuncia = new Denuncias
            {
                post_id = criarDenuncia.postid,
                usuario_id = criarDenuncia.usuarioid,
                descricao = criarDenuncia.descricao
            };
            // codigo para fazer o inserte ao banco
            var resposta = await _supabase.From<Denuncias>().Insert(denuncia);

            if (resposta.Models.Count == 0)
                return BadRequest("Falha ao criar denúncia.");

            var model = resposta.Models.First();
            // serialização certa para aparecer no swagger
            var responseDto = new DenunciaResponse
            {
                Id = model.id,
                PostId = model.post_id,
                UsuarioId = model.usuario_id,
                Descricao = model.descricao,
                DataDenuncia = model.data_denuncia
            };

            return Ok(new
            {
                mensagem = "Notificação criada com sucesso.",
                notificacao = responseDto
            });
        }

        //listar-denuncias

        [HttpGet("listar-denuncias")]
        public async Task<IActionResult> ListarDenuncias()
        {
            // fazer o select*from para listar a tabela denuncias
            var usuariosRelacionados = await _supabase
           .From<Denuncias>()
       .Get();

            var usuarios = usuariosRelacionados.Models.Select(u => new DenunciaResponse
            {
                Id = u.id,
                UsuarioId = u.usuario_id,
                PostId = u.post_id,
                Descricao = u.descricao,
                DataDenuncia = u.data_denuncia
            });
            return Ok(usuarios);
        }
        // deletar
        [HttpDelete("{id}")]
        public async Task<IActionResult> Deletar(Guid id)
        {
            var resultado = await _supabase
                .From<Denuncias>()
                .Where(n => n.id == id)
                .Single();

            if (resultado == null)
                return NotFound(new { erro = "Notificação não encontrada." });

            await _supabase.From<Denuncias>().Delete(resultado);

            return Ok(new
            {
                mensagem = "Notificação removida com sucesso.",
                idRemovido = id
            });
        }
        //dtos
        public class CriarDenuncia
        {
            public Guid postid { get; set; }
            public Guid usuarioid { get; set; }
            public string descricao { get; set; }
        }

        public class DenunciaResponse
        {
            public Guid Id { get; set; }
            public Guid PostId { get; set; }
            public Guid UsuarioId { get; set; }
            public string Descricao { get; set; }
            public DateTime DataDenuncia { get; set; }
        }
    }
}
