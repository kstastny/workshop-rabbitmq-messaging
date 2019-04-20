namespace OrcVillage.Messaging
{
    public class RoutingInfo
    {
        public string Exchange { get; }
        
        public string RoutingKey { get; }

        public RoutingInfo(string exchange, string routingKey)
        {
            Exchange = exchange;
            RoutingKey = routingKey;
        }
    }
}