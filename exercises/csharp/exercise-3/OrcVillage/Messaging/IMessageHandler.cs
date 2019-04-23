namespace OrcVillage.Messaging
{
    // ReSharper disable once TypeParameterCanBeVariant
    public interface IMessageHandler<T>
    {
        void Handle(T message);
    }
}