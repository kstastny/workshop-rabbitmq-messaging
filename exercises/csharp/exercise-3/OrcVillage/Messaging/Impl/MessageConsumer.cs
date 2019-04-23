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

        private State state = State.Created;

        private readonly object connLock = new object();

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

                //TODO get RabbitMQ connection

                //TODO create channel, set correct prefetch value

                //TODO create consumer and bind queues. create exclusive queue for consuming "orc preparations"


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

                //TODO deserialize message

                //TODO handle message

                //TODO ACK message

                //TODO handle exceptions correctly at each part
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