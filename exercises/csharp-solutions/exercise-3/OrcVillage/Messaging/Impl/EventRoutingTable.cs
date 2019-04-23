using System;
using OrcVillage.Messaging.Events;

namespace OrcVillage.Messaging.Impl
{
    public class EventRoutingTable : IRoutingTable<EventBase>
    {
        public RoutingInfo GetRoutingInfo(EventBase request)
        {
            switch (request)
            {
                case OrcEvent _:
                    return new RoutingInfo(MessagingConstants.EXCHANGE_EVENTS, MessagingConstants.ROUTINGKEY_ORC_EVENTS);
            }
            
            throw new NotSupportedException("Unknown event type: " + request.GetType());
        }
    }
}