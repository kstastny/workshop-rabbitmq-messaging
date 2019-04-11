namespace ExchangeConsumer.Messaging
{
    public interface IMessageHandler<in T>
    {
        //TODO Task
        void Handle(T message);
    }
}