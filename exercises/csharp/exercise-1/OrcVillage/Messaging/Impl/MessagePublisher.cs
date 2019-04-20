using System;
using System.Collections.Generic;
using System.Threading;
using OrcVillage.Messaging.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrcVillage.Messaging.Impl
{
    public class MessagePublisher : IMessagePublisher
    {
        private enum State
        {
            Created,
            Connected,
            Disposed
        }

        private readonly ConnectionProvider connectionProvider;
        private readonly ISerializer serializer;

        private string connectionName;

        private State state = State.Created;
        private IModel channel;

        private readonly object connLock = new object();
        private readonly object sendLock = new object();

        public MessagePublisher(
            ConnectionProvider connectionProvider,
            ISerializer serializer)
        {
            this.connectionProvider = connectionProvider;
            this.serializer = serializer;
        }


        public void PublishEvent(EventBase evnt)
        {
            if (state == State.Disposed)
                throw new InvalidOperationException("Already disposed, cannot make requests");

            if (state != State.Connected)
                Connect();


            //TODO publish event to
            // exchange MessagingConstants.EXCHANGE_EVENTS
            // routing key MessagingConstants.ROUTINGKEY_ORC_EVENTS
            // add custom header MessagingConstants.HEADER_SENDER equal to connectionName
            // use method channel.BasicPublish
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