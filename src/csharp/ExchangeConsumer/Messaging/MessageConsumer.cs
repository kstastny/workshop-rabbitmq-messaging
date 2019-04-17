using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ExchangeConsumer.Messaging
{
    public class MessageConsumer<T> : IDisposable
    {
        private readonly ILogger<MessageConsumer<T>> logger;
        private readonly ISerializer serializer;
        private readonly IMessageHandler<T> messageHandler;
        private readonly ConnectionProvider connectionProvider;

        private IModel channel;

        /// <summary>
        /// 0: no specific limit
        /// other numbers: limits size of message (number of octets / bytes) during prefetch
        /// </summary>
        private const uint PREFETCH_SIZE = 0;

        public MessageConsumer(
            ILogger<MessageConsumer<T>> logger,
            ISerializer serializer,
            IMessageHandler<T> messageHandler,
            ConnectionProvider connectionProvider)
        {
            this.logger = logger;
            this.serializer = serializer;
            this.messageHandler = messageHandler;
            this.connectionProvider = connectionProvider;
        }

        //TODO Consumer start parameters? contains list of queues, exchanges and keys (if empty, then use own queue), QOS, 
        //TODO maybe use two classes - CommandMessageConsumer and EventMessageConsumer? Only QOS and queues are different though
        public void Start(MessageConsumerConfiguration consumerConfiguration)
        {
            //TODO check state of consumer
            channel = CreateChannel(connectionProvider.GetOrCreateConnection());


            //see https://www.rabbitmq.com/consumer-prefetch.html and https://www.rabbitmq.com/amqp-0-9-1-reference.html
            // prefetchSize - number of prefetched messages
            // prefetchCount - maximum number of unacked messages
            // global - limit for the whole channel. Not global - limit per consumer
            channel.BasicQos(PREFETCH_SIZE, consumerConfiguration.PrefetchCount, false);


            //TODO only create the queue if there is any need for it
            //create a queue for messages
            var queueName = channel.QueueDeclare("", durable: false, exclusive: true, autoDelete: false).QueueName;

            foreach (var binding in consumerConfiguration.QueueBindings)
            {
                channel.QueueBind(binding.QueueName ?? queueName, binding.Exchange, binding.RoutingKey);
            }

            // create consumer and start consuming messages
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += ConsumerOnReceived;
            consumer.Shutdown += ConsumerOnShutdown;
            consumer.Registered += ConsumerOnRegistered;
            consumer.Unregistered += ConsumerOnUnregistered;


            //TODO show what happens when autoAck is true and we try to ACK
            channel.BasicConsume(consumer, queueName, autoAck: false);
        }

        private void ConsumerOnUnregistered(object sender, ConsumerEventArgs e)
        {
            logger.LogInformation("Consumer {0} registered", e.ConsumerTag);
        }

        private void ConsumerOnRegistered(object sender, ConsumerEventArgs e)
        {
            logger.LogInformation("Consumer {0} registered", e.ConsumerTag);
        }

        private void ConsumerOnShutdown(object sender, ShutdownEventArgs e)
        {
            logger.LogInformation("Consumer shutdown: {0} - {1} ", e.ReplyCode, e.ReplyText);
        }

        private void ConsumerOnReceived(object sender, BasicDeliverEventArgs e)
        {
            ProcessMessage(e);
        }

        private string GetHeaderAsString(IDictionary<string, object> headers, string key)
        {
            if (headers == null)
                return null;

            headers.TryGetValue(key, out var header);

            //when received, the header is just array of bytes
            if (header is byte[] headerBytes)
            {
                return Encoding.UTF8.GetString(headerBytes);
            }

            //if it is something else, we try string but most likely this will be null
            return header as string;
        }

        private void ProcessMessage(BasicDeliverEventArgs ea)
        {
            try
            {
                var sender = GetHeaderAsString(ea.BasicProperties.Headers, "x-sender");
                logger.LogDebug("Received message from `{0}`, exchange '{1}', routing key '{2}': {4}{5}",
                    sender ?? "<< UNKNOWN >>",
                    ea.Exchange, ea.RoutingKey, Environment.NewLine, Encoding.UTF8.GetString(ea.Body));

                T message;
                try
                {
                    message = serializer.Deserialize<T>(ea.Body);
                }
                catch (Exception exception)
                {
                    // the message could not be deserialized, therefore we cannot handle it and have to reject it and send to Dead-Letter Exchange
                    logger.LogError(
                        exception, "Received invalid message from exchange {0} with routing key {1}",
                        ea.Exchange, ea.RoutingKey);

                    //on reject RabbitMQ will automatically push message to dead letter exchange, see https://www.rabbitmq.com/dlx.html
                    channel.BasicReject(ea.DeliveryTag, false);
                    return;
                }

                try
                {
                    messageHandler.Handle(message);

                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception e)
                {
                    logger.LogError(
                        e, "Error sending message response from exchange {0} with routing key {1} to reply channel ",
                        ea.Exchange, ea.RoutingKey, ea.BasicProperties.ReplyTo);

                    //TODO we can Retry sending the message in case of failure
                    channel.BasicReject(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message");

                channel.BasicReject(ea.DeliveryTag, false);
            }
        }

        private IModel CreateChannel(IConnection conn)
        {
            var lChannel = conn.CreateModel();
            lChannel.BasicReturn += (s, e) =>
            {
                Console.WriteLine("Basic return - {0}: {1}", e.ReplyCode, e.ReplyText);
            };

            return lChannel;
        }

        public void Dispose()
        {
            channel?.Dispose();
        }
    }
}