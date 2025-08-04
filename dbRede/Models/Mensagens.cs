using Supabase.Postgrest.Attributes; // este é o correto
using Supabase.Postgrest.Models;
namespace dbRede.Models
{
    [Table("mensagens")]
    public class Mensagens: BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public Guid Id { get; set; }
        [Column("id_remetente")]
        public Guid id_remetente { get; set; }
        [Column("id_destinatario")]
        public Guid id_destinatario { get; set; }
        [Column("conteudo")]
        public string conteudo { get; set; }
        [Column("post_compartilhado_id")]
        public Guid? Postid { get; set; }
        [Column("story_id ")]
        public Guid? story_id { get; set; }
        [Column("lida")]
        public Boolean lida { get; set; }
        [Column("data_envio")]
        public DateTime data_envio { get; set; }
        [Column("apagada")]
        public Boolean apagada { get; set; }
    }
}
