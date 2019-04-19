using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrcVillage.Messaging.Impl
{
    public class MessageConsumer<T> : IMessageConsumer<T>
    {
        private enum State
        {
            Created,
            Running,
            Disposed
        }

        private readonly ILogger<MessageConsumer<T>> logger;
        private readonly ISerializerFactory serializerFactory;
        private readonly IMessageHandler<T> messageHandler;
        private readonly ConnectionProvider connectionProvider;

        private IModel channel;
        private string connectionName;

        private State state = State.Created;

        private object connLock = new object();

        /// <summary>
        /// 0: no specific limit
        /// other numbers: limits size of message (number of octets / bytes) during prefetch
        /// </summary>
        private const uint PREFETCH_SIZE = 0;

        public MessageConsumer(
            ILogger<MessageConsumer<T>> logger,
            ISerializerFactory serializerFactory,
            IMessageHandler<T> messageHandler,
            ConnectionProvider connectionProvider)
        {
            this.logger = logger;
            this.serializerFactory = serializerFactory;
            this.messageHandler = messageHandler;
            this.connectionProvider = connectionProvider;
        }

        public void Start(ConsumerConfiguration consumerConfiguration)
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

                var connection = connectionProvider.GetOrCreateConnection();
                connectionName = connection.ClientProvidedName;

                channel = connection.CreateModel();
                channel.CallbackException += ChannelOnCallbackException;
                channel.ModelShutdown += ChannelOnShutdown;


                //see https://www.rabbitmq.com/consumer-prefetch.html and https://www.rabbitmq.com/amqp-0-9-1-reference.html
                // prefetchSize - number of prefetched messages
                // prefetchCount - maximum number of unacked messages
                // global - limit for the whole channel. Not global - limit per consumer
                channel.BasicQos(PREFETCH_SIZE, consumerConfiguration.PrefetchCount, false);

                // create consumer
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += ConsumerOnReceived;
                consumer.Shutdown += ConsumerOnShutdown;
                consumer.Registered += ConsumerOnRegistered;
                consumer.Unregistered += ConsumerOnUnregistered;


                //create bindings and start consuming messages
                var exclusiveQueueNeeded = consumerConfiguration.QueueBindings.Any(x => x.QueueName == null);
                string exclusiveQueueName = null;
                if (exclusiveQueueNeeded)
                    exclusiveQueueName = channel.QueueDeclare("", false, true, true);


                foreach (var binding in consumerConfiguration.QueueBindings)
                {
                    channel.QueueBind(binding.QueueName ?? exclusiveQueueName, binding.Exchange, binding.RoutingKey);
                    channel.BasicConsume(consumer, exclusiveQueueName, autoAck: false);
                }


                state = State.Running;
            }
        }

        private void ProcessMessage(BasicDeliverEventArgs ea)
        {
            try
            {
                var sender = GetHeaderAsString(ea.BasicProperties.Headers, MessagingConstants.HEADER_SENDER);
                logger.LogDebug("Received message from `{0}`, exchange '{1}', routing key '{2}': {4}{5}",
                    sender ?? "<< UNKNOWN >>",
                    ea.Exchange, ea.RoutingKey, Environment.NewLine, Encoding.UTF8.GetString(ea.Body));

                T message;
                try
                {
                    var serializer = serializerFactory.CreateSerializer(ea.BasicProperties.ContentType);
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

                    //TODO Retry sending the message in case of failure
                    channel.BasicReject(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message");

                channel.BasicReject(ea.DeliveryTag, false);
            }
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

        private void ChannelOnShutdown(object sender, ShutdownEventArgs e)
        {
            logger.LogInformation("Shutdown channel: {0} - {1}", e.ReplyCode, e.ReplyText);
        }

        private void ChannelOnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            logger.LogInformation("Callback exception: " + e.Exception);
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

        public void Dispose()
        {
            channel?.Dispose();
        }
    }
}