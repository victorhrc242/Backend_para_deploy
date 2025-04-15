using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;

namespace dbRede.Models
{
    [Table("amizades")]
    public class Seguidor : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("usuario1")]
        public Guid Usuario1 { get; set; }

        [Column("usuario2")]
        public Guid Usuario2 { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("data_solicitacao")]
        public DateTime DataSolicitacao { get; set; }
    }
}
