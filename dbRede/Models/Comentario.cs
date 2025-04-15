using Supabase.Postgrest.Attributes; // este é o correto
using Supabase.Postgrest.Models;
namespace dbRede.Models
{
    [Table("comentarios")]
    public class Comentario : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("post_id")]
        public Guid PostId { get; set; }

        [Column("autor_id")]
        public Guid AutorId { get; set; }

        [Column("conteudo")]
        public string Conteudo { get; set; }

        [Column("data_comentario")]
        public DateTime DataComentario { get; set; }
    }
}
