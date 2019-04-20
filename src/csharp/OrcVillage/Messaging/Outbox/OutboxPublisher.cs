using System;
using System.Text;
using OrcVillage.Database;
using OrcVillage.Messaging.Commands;
using OrcVillage.Messaging.Events;

namespace OrcVillage.Messaging.Outbox
{
    public class OutboxPublisher : IMessagePublisher
    {
        private readonly IRoutingTable<EventBase> eventRoutingTable;
        private readonly IRoutingTable<CommandBase> commandRoutingTable;
        private readonly ISerializer serializer;
        private readonly VillageDbContext dbContext;

        public OutboxPublisher(
            IRoutingTable<EventBase> eventRoutingTable,
            IRoutingTable<CommandBase> commandRoutingTable,
            ISerializer serializer,
            VillageDbContext dbContext)
        {
            this.eventRoutingTable = eventRoutingTable;
            this.commandRoutingTable = commandRoutingTable;
            this.serializer = serializer;
            this.dbContext = dbContext;
        }


        public void PublishEvent(EventBase evnt)
        {
            var routingInfo = eventRoutingTable.GetRoutingInfo(evnt);
            var payload = serializer.Serialize(evnt);

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                //NOTE: this is a simplification for demo purposes, normally the Body in DB would have to be saved as byte array to support binary message formats 
                Body = Encoding.UTF8.GetString(payload),
                Exchange = routingInfo.Exchange,
                RoutingKey = routingInfo.RoutingKey,
                ContentType = serializer.ContentType,
                SentDateTime = null,
                PublishDateTime = DateTime.Now
            };

            dbContext.Add(outboxMessage);
        }

        public void PublishCommand(CommandBase command)
        {
            var routingInfo = commandRoutingTable.GetRoutingInfo(command);
            var payload = serializer.Serialize(command);

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                //NOTE: this is a simplification for demo purposes, normally the Body in DB would have to be saved as byte array to support binary message formats 
                Body = Encoding.UTF8.GetString(payload),
                Exchange = routingInfo.Exchange,
                RoutingKey = routingInfo.RoutingKey,
                ContentType = serializer.ContentType,
                SentDateTime = null,
                PublishDateTime = DateTime.Now
            };

            dbContext.Add(outboxMessage);
        }

        public void PublishPoisonMessage(CommandBase command)
        {
            var routingInfo = commandRoutingTable.GetRoutingInfo(command);
            var payload = Encoding.UTF8.GetBytes("This is poison }");

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                //NOTE: this is a simplification for demo purposes, normally the Body in DB would have to be saved as byte array to support binary message formats 
                Body = Encoding.UTF8.GetString(payload),
                Exchange = routingInfo.Exchange,
                RoutingKey = routingInfo.RoutingKey,
                ContentType = serializer.ContentType,
                SentDateTime = null,
                PublishDateTime = DateTime.Now
            };

            dbContext.Add(outboxMessage);
        }

        public void Dispose()
        {
        }
    }
}