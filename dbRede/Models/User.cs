using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;

[Table("users")]
public class User : BaseModel
{

    // aqui  não tem o id pois no banco esta programado para salvar o id altomaticamente
    // o id esta false pois ele não precisara ser  prenchido manualmente eo
    // jsonignore esta ai para por algun motivo,   e tavles não precisara deles, mas eles ta ai
    // pq quando testei e deu certo eles estavam ai e eu não quero tirar pois pode  dar erro
    [PrimaryKey("id", false)]
    [JsonIgnore]
    public Guid id { get; set; }
    [Column("nome")]
    public string Nome { get; set; }

    [Column("email")]
    public string Email { get; set; }

    [Column("senha")]
    public string Senha { get; set; }
    [Column("foto_perfil")]
    public string FotoPerfil { get; set; }
    [Column("biografia")]
    public string biografia { get; set; }
    [Column("dataaniversario")]
    public string dataaniversario { get; set; }
}
