using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrcVillage.Database
{
    [Table("Outbox")]
    public class OutboxMessage
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("MessageId")]
        public Guid Id { get; set; }

        [Column("Exchange")]
        public string Exchange { get; set; }

        [Column("RoutingKey")]
        public string RoutingKey { get; set; }

        [Column("Body")]
        public string Body { get; set; }

        [Column("ContentType")]
        public string ContentType { get; set; }
        
        [Column("PublishDateTime")]
        public DateTime PublishDateTime { get; set; }
        
        [Column("SentDateTime")]
        public DateTime? SentDateTime { get; set; }
    }
}