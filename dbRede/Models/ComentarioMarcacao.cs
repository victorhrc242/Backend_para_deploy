using Supabase.Postgrest.Attributes; // este é o correto
using Supabase.Postgrest.Models;

[Table("comentarios_marcacoes")]
public class ComentarioMarcacao : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("comentario_id")]
    public Guid ComentarioId { get; set; }

    [Column("usuario_marcado_id")]
    public Guid UsuarioMarcadoId { get; set; }
}
