using System;
using System.Collections.Generic;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrcVillage.Database;
using OrcVillage.Generator;
using OrcVillage.Messaging;
using OrcVillage.Messaging.Commands;
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

        public double QuestFailureRate { get; set; }

        public double PreparationFailureRate { get; set; }
    }

    public class App
    {
        private readonly ConnectionProvider connectionProvider;
        private readonly AppConfiguration appConfiguration;

        private readonly IServiceScopeFactory scopeFactory;
        private readonly OutboxProcessor outboxProcessor;
        private readonly IMessageConsumer<CommandBase> commandConsumer;
        private readonly IMessageConsumer<EventBase> eventConsumer;

        private readonly OrcMother mother = new OrcMother();
        private readonly OrcChieftain chieftain = new OrcChieftain();
        private readonly Random rnd = new Random();


        /// <summary>
        /// Exchange where messages for retrying will be sent
        /// </summary>
        private readonly string retryExchange = "orcvillage.retry." + Environment.MachineName;

        /// <summary>
        /// Exchange for repeating commands
        /// </summary>
        private readonly string repeatCommandExchange = "orcvillage.repeat." + Environment.MachineName;

        /// <summary>
        /// Message will sit here until Dead-Lettered to repeat command exchange
        /// </summary>
        private readonly string retryQueue = "orcvillage.retry." + Environment.MachineName;
        //private readonly DbContextOptionsBuilder<VillageDbContext> optionsBuilder;

        public App(
            ConnectionProvider connectionProvider,
            AppConfiguration appConfiguration,
            IServiceScopeFactory scopeFactory,
            OutboxProcessor outboxProcessor,
            IMessageConsumer<CommandBase> commandConsumer,
            IMessageConsumer<EventBase> eventConsumer)

        {
            this.connectionProvider = connectionProvider;
            this.appConfiguration = appConfiguration;
            this.scopeFactory = scopeFactory;
            this.outboxProcessor = outboxProcessor;
            this.commandConsumer = commandConsumer;
            this.eventConsumer = eventConsumer;

//            optionsBuilder = new DbContextOptionsBuilder<VillageDbContext>();
//            optionsBuilder.UseSqlServer(appConfiguration.ConnectionString);
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

                    //using (var ctx = new VillageDbContext(optionsBuilder.Options))
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

        private void SendCommand(CommandBase commandBase)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                using (var ctx = scope.ServiceProvider.GetService<VillageDbContext>())
                {
                    var messagePublisher = scope.ServiceProvider.GetService<IMessagePublisher>();

                    messagePublisher.PublishCommand(commandBase);


                    ctx.SaveChanges();
                }
            }
        }

        private void SendPoisonMessage()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                using (var ctx = scope.ServiceProvider.GetService<VillageDbContext>())
                {
                    var messagePublisher = scope.ServiceProvider.GetService<IMessagePublisher>();

                    //TODO DLX for preparation queue. or better example
                    messagePublisher.PublishPoisonMessage(chieftain.Preparation());
                    messagePublisher.PublishPoisonMessage(chieftain.Quest());

                    ctx.SaveChanges();
                }
            }
        }

        private void RequestQuest()
        {
            SendCommand(chieftain.Quest());
        }

        private void RequestPreparation()
        {
            SendCommand(chieftain.Preparation());
        }

        private void DeclareExchangesAndQueues(IConnection rabbitMqConnection)
        {
            using (var channel = rabbitMqConnection.CreateModel())
            {
                channel.ExchangeDeclare(MessagingConstants.EXCHANGE_DLX, "fanout", true, false);
                channel.ExchangeDeclare(MessagingConstants.EXCHANGE_EVENTS, "direct", true, false);

                channel.ExchangeDeclare(MessagingConstants.EXCHANGE_COMMANDS, "direct", true, false);

                //DLX https://www.rabbitmq.com/dlx.html
                channel.QueueDeclare(MessagingConstants.QUEUE_QUESTS, false, false, false,
                    new Dictionary<string, object>
                    {
                        {"x-dead-letter-exchange", MessagingConstants.EXCHANGE_DLX},
                        {"x-message-ttl", MessagingConstants.QUEST_TIMEOUT_MS}
                    });
                channel.QueueBind(
                    MessagingConstants.QUEUE_QUESTS, MessagingConstants.EXCHANGE_COMMANDS,
                    MessagingConstants.ROUTINGKEY_CHIEFTAIN_QUESTS);


                //NOTE: everyone has to prepare. only one can fulfill the quest
//                channel.QueueDeclare(MessagingConstants.QUEUE_PREPARATION, false, false, false);
//                channel.QueueBind(
//                    MessagingConstants.QUEUE_PREPARATION, MessagingConstants.EXCHANGE_COMMANDS,
//                    MessagingConstants.ROUTINGKEY_CHIEFTAIN_PREPARATION);

                // retry for commands

                channel.ExchangeDeclare(retryExchange, "fanout", false, false);
                channel.QueueDeclare(retryQueue, false, false, false,
                    new Dictionary<string, object>
                    {
                        // send message to repeat exchange
                        {"x-dead-letter-exchange", repeatCommandExchange},
                        // NOTE: retry timeout is static, no backoff or jitter.
                        {"x-message-ttl", 5000}
                    });
                channel.QueueBind(retryQueue, retryExchange, "");

                // repeat commands
                channel.ExchangeDeclare(repeatCommandExchange, "direct", false, false);
                // when we repeat commands, we have to send QUESTS to normal command exchange, they can be taken by anyone
                // however, PREPARATIONS have to be handled by us only if we have to repeat them (exclusive queue is created by the consumer)
                //channel.QueueBind(MessagingConstants.QUEUE_QUESTS, repeatCommandExchange,MessagingConstants.ROUTINGKEY_CHIEFTAIN_QUESTS);
                channel.ExchangeBind(MessagingConstants.EXCHANGE_COMMANDS, repeatCommandExchange, MessagingConstants.ROUTINGKEY_CHIEFTAIN_QUESTS);
            }
        }

        public void Run()
        {
            outboxProcessor.Start();
            var rabbitMqConnection = connectionProvider.GetOrCreateConnection();

            DeclareExchangesAndQueues(rabbitMqConnection);
            StartConsumers();

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
                        case "quest":
                            RequestQuest();
                            break;
                        case "prep":
                            RequestPreparation();
                            break;
                        case "poison":
                            SendPoisonMessage();
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }

            commandConsumer.Dispose();
            eventConsumer.Dispose();
            rabbitMqConnection.Dispose();
            outboxProcessor.Dispose();
        }


        private void StartConsumers()
        {
            eventConsumer.Start(
                new ConsumerConfiguration
                {
                    PrefetchCount = 300,
                    QueueBindings = new List<QueueBinding>
                    {
                        new QueueBinding
                        {
                            Exchange = MessagingConstants.EXCHANGE_EVENTS,
                            RoutingKey = MessagingConstants.ROUTINGKEY_ORC_EVENTS
                        }
                    }
                });

            commandConsumer.Start(
                new ConsumerConfiguration
                {
                    PrefetchCount = 1,
                    QueueBindings = new List<QueueBinding>
                    {
                        new QueueBinding
                        {
                            Exchange = MessagingConstants.EXCHANGE_COMMANDS,
                            RoutingKey = MessagingConstants.ROUTINGKEY_CHIEFTAIN_QUESTS,
                            QueueName = MessagingConstants.QUEUE_QUESTS,
                            RetryDlx = retryExchange
                        },
                        new QueueBinding
                        {
                            Exchange = MessagingConstants.EXCHANGE_COMMANDS,
                            RoutingKey = MessagingConstants.ROUTINGKEY_CHIEFTAIN_PREPARATION,
                            RetryDlx = retryExchange
                        },
                        new QueueBinding
                        {
                            Exchange = repeatCommandExchange,
                            RoutingKey = MessagingConstants.ROUTINGKEY_CHIEFTAIN_PREPARATION,
                            //NOTE: for simplicity, we will not repeat failed preparations again
                            //RetryDlx = retryExchange
                        }
                    }
                });
        }
    }
}