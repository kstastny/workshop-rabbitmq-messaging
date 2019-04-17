using System;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using OrcVillage.Database;
using OrcVillage.Generator;
using OrcVillage.Messaging;
using OrcVillage.Messaging.Events;
using RabbitMQ.Client;

namespace OrcVillage
{
    public class AppConfiguration
    {
        public string ConnectionString { get; set; }

        public double DbFailureRate { get; set; }

        public double MessagingFailureRate { get; set; }
    }

    public class App
    {
        private readonly ConnectionProvider connectionProvider;
        private readonly AppConfiguration appConfiguration;
        private readonly IMessagePublisher messagePublisher;

        private readonly OrcMother mother = new OrcMother();

        private readonly DbContextOptionsBuilder<VillageDbContext> optionsBuilder;

        public App(
            ConnectionProvider connectionProvider,
            AppConfiguration appConfiguration,
            IMessagePublisher messagePublisher)

        {
            this.connectionProvider = connectionProvider;
            this.appConfiguration = appConfiguration;
            this.messagePublisher = messagePublisher;

            optionsBuilder = new DbContextOptionsBuilder<VillageDbContext>();
            optionsBuilder.UseSqlServer(appConfiguration.ConnectionString);
        }


        private string ReadCommand()
        {
            Console.Write("> ");
            return Console.ReadLine()?.Trim().ToLowerInvariant();
        }

        private void AddWarrior()
        {
            var newborn = mother.GiveBirth();

            using (var tran = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var ctx = new VillageDbContext(optionsBuilder.Options))
                {
                    //TODO random errors
                    ctx.Add(newborn);

                    messagePublisher.PublishEvent(new OrcEvent
                    {
                        Type = MessagingConstants.EVENT_TYPE_ORCEVENT,
                        OrcId = newborn.Id,
                        Name = newborn.Name,
                        Profession = newborn.Profession
                    });

                    ctx.SaveChanges();
                }

                tran.Complete();
                Console.WriteLine("New warrior was born: " + newborn.Name);
            }
        }

        private void DeclareExchangesAndQueues(IConnection rabbitMqConnection)
        {
            //TODO DLX support
            using (var channel = rabbitMqConnection.CreateModel())
            {
                //NOTE: normally the exchange would be durable
                channel.ExchangeDeclare(MessagingConstants.EXCHANGE_EVENTS, "direct", false, false);

                //TODO remove - is just for test
//                channel.QueueDeclare("test", false, false, false);
//                channel.QueueBind("test", MessagingConstants.EXCHANGE_EVENTS, MessagingConstants.ROUTINGKEY_ORC_EVENTS);
            }
        }

        public void Run()
        {
            var rabbitMqConnection = connectionProvider.GetOrCreateConnection();

            DeclareExchangesAndQueues(rabbitMqConnection);

            var cmd = "";

            while (cmd != "exit")
            {
                cmd = ReadCommand();
                try
                {
                    switch (cmd)
                    {
                        case "add":
                            AddWarrior();
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error:" + e.Message);
                }
            }
            
            rabbitMqConnection.Dispose();
        }
    }
}