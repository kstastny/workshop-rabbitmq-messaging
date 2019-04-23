using System;

namespace OrcVillage.Messaging
{
    public interface IMessageConsumer<T> : IDisposable
    {
        void Start(ConsumerConfiguration consumerConfiguration);
    }
}