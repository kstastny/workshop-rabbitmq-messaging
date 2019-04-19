namespace OrcVillage.Messaging
{
    public interface ISerializerFactory
    {
        ISerializer CreateSerializer(string contentType);
    }
}