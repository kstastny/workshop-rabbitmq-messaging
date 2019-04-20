using System;
using OrcVillage.Messaging.Commands;
using OrcVillage.Messaging.Events;

namespace OrcVillage.Messaging
{
    public interface IMessagePublisher : IDisposable
    {
        void PublishEvent(EventBase evnt);
        
        void PublishCommand(CommandBase command);
        
        void PublishPoisonMessage(CommandBase command);
    }
}