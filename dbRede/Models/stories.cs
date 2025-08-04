using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;
[Table("stories")]

public class stories:BaseModel
{
    [PrimaryKey("id", false)]
    [JsonIgnore]
    public Guid id { get; set; }
    [Column("usuario_id")]
    public Guid id_usuario { get; set; }
    [Column("conteudo_url")]
    public string conteudo_url { get; set; }
    [Column("tipo")]//video ou imagem
    public string tipo { get; set; }
    [Column("data_criacao")]
    public DateTime data_criacao { get; set; }
    [Column("data_expiracao")]
    public DateTime data_expiracao { get; set; }
    [Column("visualizacoes")]
    public int visualizacoes { get; set; }
    [Column("ativo")]
    public bool ativo { get; set; } // pode ser publico ou privado

}

