using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ExchangeConsumer.Messaging
{
    /// <summary>
    /// Simple message consumer that writes out the content of message as string
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MessageSpy<T> : IDisposable
    {
        private enum State
        {
            Created,
            Running,
            Disposed
        }

        private readonly ILogger<MessageSpy<T>> logger;
        private readonly ConnectionProvider connectionProvider;

        private IModel channel;

        private State state = State.Created;

        private readonly object connLock = new object();

        /// <summary>
        /// 0: no specific limit
        /// other numbers: limits size of message (number of octets / bytes) during prefetch
        /// </summary>
        private const uint PREFETCH_SIZE = 0;

        public MessageSpy(
            ILogger<MessageSpy<T>> logger,
            ConnectionProvider connectionProvider)
        {
            this.logger = logger;
            this.connectionProvider = connectionProvider;
        }


        public void Start(MessageSpyConfiguration spyConfiguration)
        {
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (state == State.Disposed)
                throw new InvalidOperationException("Already disposed, cannot make requests");

            if (state == State.Running)
                return;

            lock (connLock)
            {
                if (state == State.Running)
                    return;

                channel = CreateChannel(connectionProvider.GetOrCreateConnection());
                //see https://www.rabbitmq.com/consumer-prefetch.html and https://www.rabbitmq.com/amqp-0-9-1-reference.html
                // prefetchSize - number of prefetched messages
                // prefetchCount - maximum number of unacked messages
                // global - limit for the whole channel. Not global - limit per consumer
                channel.BasicQos(PREFETCH_SIZE, spyConfiguration.PrefetchCount, false);


                var queueName = channel.QueueDeclare("", durable: false, exclusive: true, autoDelete: false).QueueName;

                foreach (var binding in spyConfiguration.QueueBindings)
                {
                    channel.QueueBind(queueName, binding.Exchange, binding.RoutingKey);
                }

                // create consumer and start consuming messages
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += ConsumerOnReceived;
                consumer.Shutdown += ConsumerOnShutdown;
                consumer.Registered += ConsumerOnRegistered;
                consumer.Unregistered += ConsumerOnUnregistered;

                channel.BasicConsume(consumer, queueName, autoAck: false);
            }
        }

        private void ConsumerOnUnregistered(object sender, ConsumerEventArgs e)
        {
            logger.LogInformation("Consumer {0} unregistered", e.ConsumerTag);
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


                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
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