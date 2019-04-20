using System;
using OrcVillage.Messaging.Events;

namespace OrcVillage.Messaging
{
    public interface IMessagePublisher : IDisposable
    {
        void PublishEvent(EventBase evnt);
    }
}