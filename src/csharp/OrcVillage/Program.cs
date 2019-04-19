using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrcVillage.Database;
using OrcVillage.Messaging.Impl;
using Microsoft.EntityFrameworkCore;
using OrcVillage.Messaging;
using OrcVillage.Messaging.Commands;
using OrcVillage.Messaging.Events;
using OrcVillage.Messaging.Outbox;

namespace OrcVillage
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = SetupServices();

            serviceProvider.GetService<App>().Run();
        }

        private static ConnectionConfiguration GetConfiguration()
        {
            //TODO read from settings, aka Configuration.GetSection("RabbitRpc").Get<RabbitConfigConnection>();
            return new ConnectionConfiguration
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
                DbFailureRate = 0,
                MessagingFailureRate = 0,
                QuestFailureRate = 0.5,
                PreparationFailureRate = 0.1
            };
        }

        private static IServiceProvider SetupServices()
        {
            var appConfiguration = GetAppConfiguration();

            var services = new ServiceCollection()
                .AddSingleton(GetConfiguration())
                .AddSingleton(appConfiguration)
                .AddSingleton<App>();
                
                
                
            //serialization
            services
                //.AddSingleton<ISerializer, JsonSerializer>()
                .AddSingleton<ISerializer, JsonDomainSerializer>()
                .AddSingleton<ISerializerFactory, SerializerFactory>()
                .AddSingleton<IDomainConverter<CommandBase>, CommandDomainConverter>()
                .AddSingleton<IDomainConverter<EventBase>, EventDomainConverter>()
                ;
                


            services.AddDbContext<VillageDbContext>(c => { c.UseSqlServer(appConfiguration.ConnectionString); });

            //setup RabbitMq
            services
                .AddSingleton<ConnectionProvider>()
                .AddSingleton<IRoutingTable<CommandBase>, CommandRoutingTable>()
                .AddSingleton<IRoutingTable<EventBase>, EventRoutingTable>()
                .AddSingleton<IMessagePublisher, MessagePublisher>()
//                .AddScoped<IMessagePublisher, OutboxPublisher>()
                .AddSingleton<OutboxProcessor>()
                .AddSingleton(typeof(IMessageConsumer<>), typeof(MessageConsumer<>))
                .AddSingleton<IMessageHandler<CommandBase>, CommandHandler>()
                .AddSingleton<IMessageHandler<EventBase>, OrcEventHandler>()
                ;

            services.AddLogging(builder =>
            {
                builder.AddConsole(cnf => { cnf.IncludeScopes = true; });
                builder.SetMinimumLevel(LogLevel.Warning);
            });

            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }
    }
}