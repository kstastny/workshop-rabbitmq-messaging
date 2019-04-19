using System;
using OrcVillage.Messaging.Events;

namespace OrcVillage.Messaging.Impl
{
    public class OrcEventHandler : IMessageHandler<EventBase>
    {
        public void Handle(EventBase message)
        {
            Console.WriteLine("Event received " + message.GetType().FullName);
        }
    }
}