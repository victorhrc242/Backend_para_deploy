using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;

namespace dbRede.Models
{
    [Table("notificacoes")]
    public class Notificacao : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("usuario_id")]
        public Guid UsuarioId { get; set; }

        [Column("tipo")]
        public string Tipo { get; set; }

        [Column("referencia_id")]
        public Guid ReferenciaId { get; set; }

        [Column("mensagem")]
        public string Mensagem { get; set; }

        [Column("data_envio")]
        public DateTime DataEnvio { get; set; }
    }
}
