namespace OrcVillage.Messaging
{
    public interface IRoutingTable<TRequest>
    {
        RoutingInfo GetRoutingInfo(TRequest request);
    }
}