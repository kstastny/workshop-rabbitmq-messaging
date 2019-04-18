using System;
using ExchangeConsumer.Events;

namespace ExchangeConsumer.Messaging.Impl
{
    public class OrcEventHandler : IMessageHandler<OrcEvent>
    {
        public void Handle(OrcEvent message)
        {
            Console.WriteLine("Received event '{0}' for orc '{1}'", message.Type, message.Name);
        }
    }
}