using System;
using System.Collections.Generic;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrcVillage.Database;
using OrcVillage.Generator;
using OrcVillage.Messaging;
using OrcVillage.Messaging.Events;
using OrcVillage.Messaging.Outbox;
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

        private readonly IServiceScopeFactory scopeFactory;
        private readonly OutboxProcessor outboxProcessor;

        private readonly OrcMother mother = new OrcMother();
        private readonly Random rnd = new Random();


        public App(
            ConnectionProvider connectionProvider,
            AppConfiguration appConfiguration,
            IServiceScopeFactory scopeFactory,
            OutboxProcessor outboxProcessor)

        {
            this.connectionProvider = connectionProvider;
            this.appConfiguration = appConfiguration;
            this.scopeFactory = scopeFactory;
            this.outboxProcessor = outboxProcessor;
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void RandomFailure(string where, double failureRate)
        {
            if (rnd.NextDouble() < failureRate)
                throw new Exception("Random failure at " + where);
        }


        private string ReadCommand()
        {
            Console.Write("> ");
            return Console.ReadLine()?.Trim().ToLowerInvariant();
        }

        private void AddWarrior()
        {
            var newborn = mother.GiveBirth();
            Console.WriteLine("New warrior was born: " + newborn.Name);

            using (var tran = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var messagePublisher = scope.ServiceProvider.GetService<IMessagePublisher>();

                    using (var ctx = scope.ServiceProvider.GetService<VillageDbContext>())
                    {
                        ctx.Add(newborn);

                        RandomFailure("messaging", appConfiguration.MessagingFailureRate);

                        messagePublisher.PublishEvent(new OrcEvent
                        {
                            EventType = "born",
                            OrcId = newborn.Id,
                            Name = newborn.Name,
                            Profession = newborn.Profession
                        });

                        RandomFailure("database", appConfiguration.DbFailureRate);

                        ctx.SaveChanges();
                    }
                }

                tran.Complete();
            }
        }


        private void DeclareExchangesAndQueues(IConnection rabbitMqConnection)
        {
            using (var channel = rabbitMqConnection.CreateModel())
            {
                channel.ExchangeDeclare(MessagingConstants.EXCHANGE_EVENTS, "direct", true, false);
            }
        }

        public void Run()
        {
            outboxProcessor.Start();
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
                    Console.WriteLine("Error: " + e.Message);
                }
            }

            rabbitMqConnection.Dispose();
            outboxProcessor.Dispose();
        }
    }
}