using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using OrcVillage.Database;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrcVillage.Messaging.Outbox
{
    /// <summary>
    /// Processes messages in outbox and sends them to RabbitMQ. NOTE: in production system, this would be launched by some reliable scheduler like Quartz.NET
    /// </summary>
    public class OutboxProcessor : IDisposable
    {
        private enum State
        {
            Created,
            Connected,
            Disposed
        }

        private string connectionName;

        private State state = State.Created;
        private IModel channel;

        private readonly object connLock = new object();
        private readonly object sendLock = new object();


        private readonly IServiceScopeFactory scopeFactory;
        private readonly ConnectionProvider connectionProvider;

        private readonly Timer timer = new Timer();

        public OutboxProcessor(
            IServiceScopeFactory scopeFactory,
            ConnectionProvider connectionProvider)
        {
            this.scopeFactory = scopeFactory;
            this.connectionProvider = connectionProvider;
            timer.Interval = 1000;
            timer.Elapsed += TimerOnElapsed;
        }


        public void Start()
        {
            timer.Start();
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                using (var ctx = scope.ServiceProvider.GetService<VillageDbContext>())
                {
                    var query =
                        from msg in ctx.Set<OutboxMessage>()
                        where msg.SentDateTime == null
                        select msg;

                    var messages = query.ToList();
                    PublishMessages(messages);

                    foreach (var outboxMessage in messages)
                    {
                        outboxMessage.SentDateTime = DateTime.Now;
                    }

                    ctx.SaveChanges();
                }
            }
        }

        private void PublishMessages(IList<OutboxMessage> messages)
        {
            if (messages == null || messages.Count == 0)
                return;

            if (state == State.Disposed)
                throw new InvalidOperationException("Already disposed, cannot make requests");

            if (state != State.Connected)
                Connect();

            lock (sendLock)
            {
                foreach (var msgGroup in messages.GroupBy(m => m.ContentType))
                {
                    var batch = channel.CreateBasicPublishBatch();

                    var requestProperties = channel.CreateBasicProperties();
                    requestProperties.ContentType = msgGroup.Key;

                    requestProperties.Headers = new Dictionary<string, object>();
                    requestProperties.Headers[MessagingConstants.HEADER_SENDER] = connectionName;

                    foreach (var msg in msgGroup)
                    {
                        //TODO each send/bulk send will be a TCS, similar to RPC client. if possible
                        //TODO publish confirms - counting messages https://www.rabbitmq.com/confirms.html

                        batch.Add(
                            msg.Exchange, msg.RoutingKey, true, requestProperties, Encoding.UTF8.GetBytes(msg.Body));
                    }

                    batch.Publish();
                }
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

        public void Dispose()
        {
            timer.Dispose();
            channel?.Dispose();
        }
    }
}