using System;
using OrcVillage.Messaging.Events;

namespace OrcVillage.Messaging.Impl
{
    public class EventHandler : IMessageHandler<EventBase>
    {
        public void Handle(EventBase message)
        {
            Console.WriteLine("Event received " + message);
        }
    }
}