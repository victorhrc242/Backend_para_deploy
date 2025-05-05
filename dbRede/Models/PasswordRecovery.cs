using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;

[Table("password_recovery")]
public class PasswordRecovery:BaseModel
{
    [PrimaryKey("id", false)]
    [JsonIgnore]
    public Guid Id { get; set; }
    [Column("user_id")]
    public Guid UserId { get; set; }
    [Column("recovery_code")]
    public string RecoveryCode { get; set; }
    [Column("expiration")]
    public DateTime Expiration { get; set; }
    [Column("is_used")]
    public bool IsUsed { get; set; }
}
