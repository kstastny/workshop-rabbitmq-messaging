using System;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrcVillage.Messaging
{
    public sealed class ConnectionProvider : IDisposable
    {
        private readonly ILogger<ConnectionProvider> logger;
        private readonly Configuration configuration;

        private readonly object lockObj = new object();

        private volatile IConnection connection = null;

        public ConnectionProvider(
            ILogger<ConnectionProvider> logger,
            Configuration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        public IConnection GetOrCreateConnection()
        {
            if (connection != null)
                return connection;

            lock (lockObj)
            {
                if (connection != null)
                    return connection;

                var conn = CreateConnection();

                conn.RecoverySucceeded += Connection_RecoverySucceeded;
                conn.CallbackException += Connection_CallbackException;
                conn.ConnectionBlocked += Connection_ConnectionBlocked;
                conn.ConnectionShutdown += Connection_ConnectionShutdown;
                conn.ConnectionUnblocked += Connection_ConnectionUnblocked;

                connection = conn;
            }

            return connection;
        }



        private IConnection CreateConnection()
        {
            var connectionFactory = new ConnectionFactory
            {
                HostName = configuration.Host,
                Port = configuration.Port,
                UserName = configuration.Username,
                Password = configuration.Password,
                VirtualHost = configuration.VHost,
                AutomaticRecoveryEnabled = true
            };

            var conn = connectionFactory.CreateConnection(configuration.ConnectionName);

            return conn;
        }

        private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            logger.LogInformation("Connection {0} shutdown: {1} ", GetConnectionInfo(sender), e.ReplyText);
            Console.WriteLine("Connection {0} shutdown: {1} ", GetConnectionInfo(sender), e.ReplyText);
//            if (sender is IConnection conn)
//            {
//                conn.CallbackException -= Connection_CallbackException;
//                conn.ConnectionBlocked -= Connection_ConnectionBlocked;
//                conn.ConnectionShutdown -= Connection_ConnectionShutdown;
//                conn.ConnectionUnblocked -= Connection_ConnectionUnblocked;
//                conn.RecoverySucceeded -= Connection_RecoverySucceeded;
//            }
        }


        private void Connection_ConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            logger.LogInformation("Connection {0} blocked: {1} ", GetConnectionInfo(sender), e.Reason);
        }

        private void Connection_CallbackException(object sender, CallbackExceptionEventArgs e)
        {
            logger.LogInformation("Connection {0} CallbackException: ", GetConnectionInfo(sender), e);
            
        }

        private void Connection_ConnectionUnblocked(object sender, EventArgs e)
        {
            logger.LogInformation("Connection {0} unblocked: {1} ", GetConnectionInfo(sender));
        }
        
        private void Connection_RecoverySucceeded(object sender, EventArgs e)
        {
            logger.LogInformation("Connection {0} recovery succeeded: {1} ", GetConnectionInfo(sender));
            Console.WriteLine("Connection recovery succeeded: {0} ", GetConnectionInfo(sender));
        }

        private string GetConnectionInfo(object sender)
        {
            if (!(sender is IConnection conn))
                return "";

            return conn.ClientProvidedName + "," + conn.Endpoint;
        }

        public void Dispose()
        {
            connection?.Dispose();
            connection = null;
        }
    }
}