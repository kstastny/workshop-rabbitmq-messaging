using System;
using System.Collections.Generic;
using ExchangeConsumer.DTO;
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

            serviceProvider.GetService<MessageConsumer<VehicleEventDto>>().Start(
                new MessageConsumerConfiguration
                {
                    PrefetchCount = 300,
                    QueueBindings = new List<QueueBinding>
                    {
                        new QueueBinding
                        {
                            Exchange = "vehicle-repository.events",
                            RoutingKey = "vehicle-event"
                        }
                    }
                });

            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
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
//setup our DI
            var services = new ServiceCollection()
                .AddSingleton<Configuration>(GetConfiguration())
                .AddSingleton<ConnectionProvider>()
                .AddSingleton(typeof(MessageConsumer<>))
                .AddSingleton<IMessageHandler<VehicleEventDto>, VehicleEventHandler>()
                .AddSingleton<ISerializer, JsonSerializer>();


            services.AddLogging((builder) =>
            {
               // builder.AddConfiguration(configuration);
                builder.AddConsole(cnf => { cnf.IncludeScopes = true; });
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }
    }
}