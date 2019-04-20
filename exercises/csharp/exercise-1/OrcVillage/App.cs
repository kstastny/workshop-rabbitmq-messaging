using System;
using System.Collections.Generic;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    }

    public class App
    {
        private readonly ConnectionProvider connectionProvider;

        private readonly IServiceScopeFactory scopeFactory;

        private readonly OrcMother mother = new OrcMother();

        private readonly DbContextOptionsBuilder<VillageDbContext> optionsBuilder;

        public App(
            ConnectionProvider connectionProvider,
            AppConfiguration appConfiguration,
            IServiceScopeFactory scopeFactory
        )
        {
            this.connectionProvider = connectionProvider;
            this.scopeFactory = scopeFactory;

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
            Console.WriteLine("New warrior was born: " + newborn.Name);

            using (var tran = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var messagePublisher = scope.ServiceProvider.GetService<IMessagePublisher>();

                    using (var ctx = new VillageDbContext(optionsBuilder.Options))
                    {
                        ctx.Add(newborn);

                        messagePublisher.PublishEvent(new OrcEvent
                        {
                            EventType = "born",
                            OrcId = newborn.Id,
                            Name = newborn.Name,
                            Profession = newborn.Profession
                        });

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
        }
    }
}