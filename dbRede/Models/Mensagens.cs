using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace dbRede.Models
{
    public class Mensagens
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)] // ID interno do MongoDB
        public string Id { get; set; }

        [BsonElement("id_remetente")]
        public string id_remetente { get; set; } // UUID convertido para string

        [BsonElement("id_destinatario")]
        public string id_destinatario { get; set; }

        [BsonElement("conteudo")]
        public string conteudo { get; set; }

        [BsonElement("post_compartilhado_id")]
        public string PostId { get; set; } // Pode ser null

        [BsonElement("story_id")]
        public string StoryId { get; set; } // Pode ser null

        [BsonElement("lida")]
        public bool lida { get; set; } = false;

        [BsonElement("data_envio")]
        public DateTime data_envio { get; set; } = DateTime.UtcNow;

        [BsonElement("apagada")]
        public bool Apagada { get; set; } = false;
    }
}
