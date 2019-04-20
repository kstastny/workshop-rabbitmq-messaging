using System;
using System.Collections.Generic;
using ExchangeConsumer.Events;
using ExchangeConsumer.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExchangeConsumer
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = SetupServices();

            serviceProvider.GetService<MessageSpy<OrcEvent>>().Start(
                new MessageSpyConfiguration
                {
                    PrefetchCount = 300,
                    QueueBindings = new List<QueueBinding>
                    {
                        new QueueBinding
                        {
                            Exchange = "orcvillage.events",
                            RoutingKey = "orcevent"
                        },
                        new QueueBinding
                        {
                            Exchange = "orcvillage.commands",
                            RoutingKey = "quest"
                        },
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

        private static Configuration GetConfiguration(IConfiguration config)
        {
            var connectionConfiguration =
                config.GetSection("RabbitMq").Get<Configuration>();

            if (connectionConfiguration == null)
                throw new Exception("Missing or invalid RabbitMQ configuration");

            connectionConfiguration.ConnectionName = "cs-exchange-consumer @ " + Environment.MachineName;

            return connectionConfiguration;
        }

        private static IServiceProvider SetupServices()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables()
                .Build();


            var services = new ServiceCollection()
                .AddSingleton(GetConfiguration(config))
                .AddSingleton<ConnectionProvider>()
                .AddSingleton(typeof(MessageSpy<>));


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