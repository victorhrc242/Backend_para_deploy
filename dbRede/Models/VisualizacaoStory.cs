using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
namespace dbRede.Models
{
    [Table("visualizacao_story")]
    public class VisualizacaoStory:BaseModel
    {
        [PrimaryKey("id", false)] // se o banco gera o ID automaticamente
        [Column("id")]
        public Guid id { get; set; }
        [Column("usuario_id")]
        public Guid usuario_id { get; set; }
        [Column("story_id")]
        public Guid story_id { get; set; }
        [Column("data_visualizacao")]
        public DateTime data_visualizacao { get; set; }
        [Column("tempo_em_segundos")]
        public int tempo_em_segundos { get; set; }
    }
}
