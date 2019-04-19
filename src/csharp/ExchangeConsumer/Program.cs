using System;
using System.Collections.Generic;
using ExchangeConsumer.Events;
using ExchangeConsumer.Messaging;
using ExchangeConsumer.Messaging.Impl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExchangeConsumer
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = SetupServices();
            //TODO declare exchanges and shared queues. including one shared DLX

            //TODO separate consumer for Commands! and for DLX!
            serviceProvider.GetService<MessageConsumer<OrcEvent>>().Start(
                new MessageConsumerConfiguration
                {
                    PrefetchCount = 300,
                    QueueBindings = new List<QueueBinding>
                    {
                        new QueueBinding
                        {
                            Exchange = "orcvillage.events",
                            RoutingKey = "orcevent"
                        },
//                        new QueueBinding
//                        {
//                            Exchange = "orcvillage.commands",
//                            RoutingKey = "quest"
//                        },
                        new QueueBinding
                        {
                            Exchange = "orcvillage.commands",
                            RoutingKey = "preparationtask"
                        },                        
                        new QueueBinding
                        {
                            Exchange = "orcvillage.dlx",
                            RoutingKey = ""
                        },
                    }
                });

            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();

            serviceProvider.GetService<ConnectionProvider>().Dispose();
        }

        private static Configuration GetConfiguration()
        {
            //TODO read from settings, aka Configuration.GetSection("RabbitRpc").Get<RabbitConfigConnection>();
            return new Configuration
            {
                Host = "stastnyk",
                Port = 5672,
                VHost = "/",
                Username = "workshop",
                Password = "inasproboate",
                ConnectionName = "cs-exchange-consumer @ " + Environment.MachineName
            };
        }

        private static IServiceProvider SetupServices()
        {
            var services = new ServiceCollection()
                .AddSingleton(GetConfiguration())
                .AddSingleton<ConnectionProvider>()
                .AddSingleton(typeof(MessageConsumer<>))
                .AddSingleton<IMessageHandler<OrcEvent>, OrcEventHandler>()
                .AddSingleton<ISerializer, JsonSerializer>();


            services.AddLogging(builder =>
            {
                builder.AddConsole(cnf => { cnf.IncludeScopes = true; });
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }
    }
}