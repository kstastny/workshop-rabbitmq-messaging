using System;

namespace ExchangeConsumer.Events
{
    public class OrcEvent : EventBase
    {
        public string Type { get; set; }
        public Guid OrcId { get; set; }
        public string Name { get; set; }
        public string Profession { get; set; }
    }
}