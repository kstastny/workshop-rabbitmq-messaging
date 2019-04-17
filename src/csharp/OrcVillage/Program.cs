using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrcVillage.Database;
using OrcVillage.Messaging.Impl;
using Microsoft.EntityFrameworkCore;
using OrcVillage.Messaging;
using OrcVillage.Messaging.Events;

namespace OrcVillage
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = SetupServices();

            serviceProvider.GetService<App>().Run();
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
                ConnectionName = "cs-orc-village @ " + Environment.MachineName
            };
        }

        private static AppConfiguration GetAppConfiguration()
        {
            return new AppConfiguration
            {
                ConnectionString =
                    @"Server=(LocalDB)\messaging;Initial Catalog=messaging_samples;Persist Security Info=False;Integrated security=False;User ID=messaging;Password=Vo60&8cV7erE;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;",
                DbFailureRate = 0.25,
                MessagingFailureRate = 0.25
            };
        }

        private static IServiceProvider SetupServices()
        {
            var appConfiguration = GetAppConfiguration();
//setup our DI
            var services = new ServiceCollection()
                .AddSingleton(GetConfiguration())
                .AddSingleton(appConfiguration)
                .AddSingleton<ConnectionProvider>()
//                .AddSingleton(typeof(MessageConsumer<>))
//                .AddSingleton<IMessageHandler<VehicleEventDto>, VehicleEventHandler>()
                .AddSingleton<ISerializer, JsonSerializer>()
                .AddSingleton<App>();


            //services.AddDbContext<VillageDbContext>(c => { c.UseSqlServer(appConfiguration.ConnectionString); });

            //setup RabbitMq
            services
                .AddSingleton<IRoutingTable<EventBase>, EventRoutingTable>()
                .AddSingleton<IMessagePublisher, MessagePublisher>();

            services.AddLogging(builder =>
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