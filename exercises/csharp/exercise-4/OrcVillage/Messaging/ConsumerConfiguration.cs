using System.Collections.Generic;

namespace OrcVillage.Messaging
{
    public class ConsumerConfiguration
    {
        public ushort PrefetchCount { get; set; }

        public ICollection<QueueBinding> QueueBindings { get; set; }

        public ConsumerConfiguration()
        {
            //see https://www.rabbitmq.com/confirms.html - recommended QOS value is 100-300
            PrefetchCount = 100;
            QueueBindings = new List<QueueBinding>();
        }
    }
    
    public class QueueBinding
    {
        /// <summary>
        /// If null, bounds to consumers exclusive queue
        /// </summary>
        public string QueueName { get; set; }

        public string Exchange { get; set; }
        
        public string RoutingKey { get; set; }
    }
}