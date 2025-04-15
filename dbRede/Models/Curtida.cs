using Supabase.Postgrest.Attributes; // este é o correto
using Supabase.Postgrest.Models;
using System;

namespace dbRede.Models
{
    [Table("curtidas")] // nome da tabela no Supabase
    public class Curtida : BaseModel
    {
        [PrimaryKey("id", false)] // false: não é autoincremento
        [Column("id")]
        public Guid Id { get; set; }

        [Column("post_id")]
        public Guid PostId { get; set; }

        [Column("usuario_id")]
        public Guid UsuarioId { get; set; }

        [Column("data_curtiu")]
        public DateTime DataCurtiu { get; set; }
    }
}
