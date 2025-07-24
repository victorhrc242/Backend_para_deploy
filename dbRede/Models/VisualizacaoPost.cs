using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;
namespace dbRede.Models
{
    [Table("visualizacoes_posts")]
    public class VisualizacaoPost : BaseModel
    {
        [PrimaryKey("id", false)]
        [JsonIgnore]
        public  Guid  id { get; set; }
        [Column("post_id")]
        public Guid post_id { get; set; }
        [Column("usuario_id")]
        public Guid usuario_id { get; set; }
        [Column("data_visualizacao")]
        public DateTime data_visualizacao { get; set; }
    }
}
