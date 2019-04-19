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
        private readonly ISerializer serializer;
        private readonly VillageDbContext dbContext;

        public OutboxPublisher(
            IRoutingTable<EventBase> eventRoutingTable,
            ISerializer serializer,
            VillageDbContext dbContext)
        {
            this.eventRoutingTable = eventRoutingTable;
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
            //TODO
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}