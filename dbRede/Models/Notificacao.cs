using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace dbRede.Models
{
    public class Notificacao
    {

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("usuario_id")]
        public string UsuarioId { get; set; }

        [BsonElement("tipo")]
        public string Tipo { get; set; }

        [BsonElement("referencia_id")]
        public string ReferenciaId { get; set; }

        [BsonElement("usuario_remetente_id")]
        public string UsuarioRemetenteId { get; set; }

        [BsonElement("mensagem")]
        public string Mensagem { get; set; }

        [BsonElement("data_envio")]
        public DateTime DataEnvio { get; set; } = DateTime.UtcNow;

    }
}
