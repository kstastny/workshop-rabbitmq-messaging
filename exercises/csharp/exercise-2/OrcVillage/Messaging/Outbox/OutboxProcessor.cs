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
                // TODO reliably send the messages in batch. 
                // TODO use publisher confirms
            }
        }

        #region Connection

        private void Connect()
        {
            //TODO connect to RabbitMQ and initialize channel
        }

        private void ChannelOnBasicNacks(object sender, BasicNackEventArgs e)
        {
            Console.WriteLine("Basic NACKS, delivery tag {0}, multiple = {1} ", e.DeliveryTag, e.Multiple);
        }

        private void ChannelOnBasicAcks(object sender, BasicAckEventArgs e)
        {
            Console.WriteLine("Basic ACKS, delivery tag {0}, multiple = {1} ", e.DeliveryTag, e.Multiple);
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
            //https://stackoverflow.com/questions/6386117/rabbitmq-use-of-immediate-and-mandatory-bits
            // either unroutable mandatory message or immediate message that cannot be processed right away 
            Console.WriteLine("Received basic return. Routing key {0}, reply {1}:{2}. MessageId {3}",
                e.RoutingKey,
                e.ReplyCode,
                e.ReplyText,
                e.BasicProperties.MessageId);
        }

        #endregion

        public void Dispose()
        {
            timer.Dispose();
            channel?.Dispose();
        }
    }
}