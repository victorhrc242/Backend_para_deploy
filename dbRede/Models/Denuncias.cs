using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;

namespace dbRede.Models
{
    [Table("denuncias")]
    public class Denuncias:BaseModel
    {
        [PrimaryKey("id", false)]
        [JsonIgnore]
        public Guid id { get; set; }
        [Column("post_id")]
        public Guid post_id { get; set; }
        [Column("usuario_id")]
        public Guid usuario_id { get; set; }
        [Column("descricao")]
        public string descricao { get; set; }
        [Column("data_denuncia")]
        public DateTime data_denuncia { get; set; }

    }
}
