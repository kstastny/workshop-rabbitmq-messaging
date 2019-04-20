using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrcVillage.Database;
using OrcVillage.Messaging.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        private static ConnectionConfiguration GetConfiguration(IConfiguration config)
        {
            var connectionConfiguration =
                config.GetSection("RabbitMq").Get<ConnectionConfiguration>();

            if (connectionConfiguration == null)
                throw new Exception("Missing or invalid RabbitMQ configuration");

            connectionConfiguration.ConnectionName = "cs-orc-village @ " + Environment.MachineName;

            return connectionConfiguration;
        }

        private static AppConfiguration GetAppConfiguration(IConfiguration config)
        {
            var appConfiguration =
                config.GetSection("Application").Get<AppConfiguration>();

            if (appConfiguration == null)
                throw new Exception("Missing or invalid application configuration");


            appConfiguration.ConnectionString = config["Database:ConnectionString"];

            return appConfiguration;
        }

        private static IServiceProvider SetupServices()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables()
                .Build();


            var appConfiguration = GetAppConfiguration(config);

            var services = new ServiceCollection()
                .AddSingleton(GetConfiguration(config))
                .AddSingleton(appConfiguration)
                .AddSingleton<App>();


            //serialization
            services
                .AddSingleton<ISerializer, JsonSerializer>()
                ;


            services.AddDbContext<VillageDbContext>(c => { c.UseSqlServer(appConfiguration.ConnectionString); });

            //setup RabbitMq
            services
                .AddSingleton<ConnectionProvider>()
                .AddSingleton<IRoutingTable<EventBase>, EventRoutingTable>()
                .AddSingleton<IMessagePublisher, MessagePublisher>()
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