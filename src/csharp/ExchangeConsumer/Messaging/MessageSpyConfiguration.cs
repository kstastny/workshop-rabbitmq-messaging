using System.Collections.Generic;

namespace ExchangeConsumer.Messaging
{
    public class MessageSpyConfiguration
    {
        public ushort PrefetchCount { get; set; }

        public ICollection<QueueBinding> QueueBindings { get; set; }

        public MessageSpyConfiguration()
        {
            //see https://www.rabbitmq.com/confirms.html - recommended QOS value is 100-300
            PrefetchCount = 100;
            QueueBindings = new List<QueueBinding>();
        }
    }

    public class QueueBinding
    {
        public string Exchange { get; set; }

        public string RoutingKey { get; set; }
    }
}