using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace dbRede.Models
{
    [Table("posts")]
    public class Post : BaseModel
    {
        [PrimaryKey("id", false)] // se o banco gera o ID automaticamente
        [Column("id")]
        public Guid Id { get; set; }

        [Column("autor_id")]
        public Guid AutorId { get; set; }

        [Column("conteudo")]
        public string Conteudo { get; set; }

        [Column("imagem")]
        public string Imagem { get; set; }

        [Column("data_postagem")]
        public DateTime DataPostagem { get; set; }

        [Column("curtidas")]
        public int Curtidas { get; set; }

        [Column("comentarios")]
        public int Comentarios { get; set; }

        [Column("tags")]
        public List<string> Tags { get; set; }
        [IgnoreDataMember]
        public User Usuarios { get; set; } // relacionamento
    }
}
