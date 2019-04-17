using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using OrcVillage.Database;
using OrcVillage.Messaging.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrcVillage.Messaging.Impl
{
    public class Outbox : IMessagePublisher
    {
        private enum State
        {
            Created,
            Connected,
            Disposed
        }

        private readonly IRoutingTable<EventBase> eventRoutingTable;
        private readonly ConnectionProvider connectionProvider;
        private readonly ISerializer serializer;
        private readonly VillageDbContext dbContext;

        private string connectionName;

        private State state = State.Created;
        private IModel channel;

        private readonly object connLock = new object();
        private readonly object sendLock = new object();

        public Outbox(
            IRoutingTable<EventBase> eventRoutingTable,
            ConnectionProvider connectionProvider,
            ISerializer serializer,
            VillageDbContext dbContext)
        {
            this.eventRoutingTable = eventRoutingTable;
            this.connectionProvider = connectionProvider;
            this.serializer = serializer;
            this.dbContext = dbContext;
        }


        public void PublishEvent(EventBase evnt)
        {
            //TODO do not connect at all - this will be done by background job - OutboxPublisher. It has to use publisher confirms - each send/bulk send will be a TCS, similar to RPC client. if possible
            //TODO publish confirms - counting messages https://www.rabbitmq.com/confirms.html
            if (state == State.Disposed)
                throw new InvalidOperationException("RPC client was already disposed, cannot make requests");

            if (state != State.Connected)
                Connect();

            lock (sendLock)
            {
                var routingInfo = eventRoutingTable.GetRoutingInfo(evnt);
                var payload = serializer.Serialize(evnt);

//                var requestProperties = channel.CreateBasicProperties();
//                requestProperties.ContentType = serializer.ContentType;
//
//                requestProperties.Headers = new Dictionary<string, object>();
//                requestProperties.Headers[MessagingConstants.HEADER_SENDER] = connectionName;

//                channel.BasicPublish(
//                    routingInfo.Exchange,
//                    routingInfo.RoutingKey,
//                    body: payload,
//                    basicProperties: requestProperties,
//                    mandatory: true);

                var t = channel.CreateBasicPublishBatch();
                    //t.Add(...);
                    t.Publish();

                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    //NOTE: this is a simplification for demo purposes, normally the Body in DB would have to be saved as byte array to support binary message formats 
                    Body = Encoding.UTF8.GetString(payload),
                    Exchange = routingInfo.Exchange,
                    RoutingKey = routingInfo.RoutingKey,
                    ContentType = serializer.ContentType,
                    SentDateTime = null,
                    PublishDateTime = DateTime.Now
                };

                dbContext.Add(outboxMessage);

                //NOTE: again, just simplification
                dbContext.SaveChanges();
            }
        }

        #region Connection

        private void Connect()
        {
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (state == State.Connected)
                return;

            if (state == State.Disposed)
                throw new InvalidOperationException("Publisher was already disposed, cannot reconnect");

            lock (connLock)
            {
                if (state != State.Created)
                    return;

                var connection = connectionProvider.GetOrCreateConnection();
                connectionName = connection.ClientProvidedName;

                channel = connection.CreateModel();
                channel.BasicReturn += ChannelOnBasicReturn;
                channel.CallbackException += ChannelOnCallbackException;
                channel.ModelShutdown += ChannelOnShutdown;

                state = State.Connected;
            }
        }

        private void ChannelOnShutdown(object sender, ShutdownEventArgs e)
        {
            Console.WriteLine("Shutdown channel: {0} - {1}", e.ReplyCode, e.ReplyText);
        }

        private void ChannelOnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            Console.WriteLine("Callback exception: " + e.Exception);
        }

        private void ChannelOnBasicReturn(object sender, BasicReturnEventArgs e)
        {
            Console.WriteLine("Received basic return. Routing key {0}, reply {1}:{2}",
                e.RoutingKey,
                e.ReplyCode,
                e.ReplyText);
        }

        #endregion

        #region IDisposable implementation

        //https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (state == State.Disposed)
                return;

            if (disposing)
            {
                channel?.Dispose();
                channel = null;
            }

            state = State.Disposed;
        }

        #endregion
    }
}