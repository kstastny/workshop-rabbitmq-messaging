using System;
using System.Text;
using OrcVillage.Database;
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
            //TODO convert event to OutboxMessage and add to dbContext
            OutboxMessage outboxMessage = null;

            dbContext.Add(outboxMessage);
        }

        public void Dispose()
        {
        }
    }
}