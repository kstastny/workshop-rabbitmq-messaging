namespace ExchangeConsumer.Messaging
{
    public interface ISerializer
    {
        byte[] Serialize<T>(T obj);

        T Deserialize<T>(byte[] obj);
    }
}