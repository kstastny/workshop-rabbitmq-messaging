namespace OrcVillage.Messaging
{
    public interface ISerializer
    {
        string ContentType { get; }
        
        byte[] Serialize<T>(T obj);

        T Deserialize<T>(byte[] obj);
    }
}