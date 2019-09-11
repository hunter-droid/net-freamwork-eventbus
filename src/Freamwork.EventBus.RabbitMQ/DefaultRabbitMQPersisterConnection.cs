using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.IO;
using System.Net.Sockets;
using Unity;

namespace Freamwork.EventBus.RabbitMQ
{
    public class DefaultRabbitMQPersistentConnection
       : IRabbitMQPersistentConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger _logger;
        private readonly TimeSpan[] _timeSpans;
        IConnection _connection;
        bool _disposed;

        object sync_root = new object();

        public DefaultRabbitMQPersistentConnection(IConnectionFactory connectionFactory,TimeSpan[] timeSpans)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException("connectionFactory");
            _logger = Providers.Providers.Provider.Resolve<ILogger>() ?? throw new ArgumentNullException("logger");
            _timeSpans = timeSpans;
        }

        public bool IsConnected
        {
            get
            {
                return _connection != null && _connection.IsOpen && !_disposed;
            }
        }

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            return _connection.CreateModel();
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                _connection.Dispose();
            }
            catch (IOException ex)
            {
                _logger.Error("Dispose", "DefaultRabbitMQPersistentConnection", "dispose error", ex);
            }
        }

        public bool TryConnect()
        {
            _logger.Info("TryConnect", "DefaultRabbitMQPersistentConnection", "RabbitMQ Client is trying to connect");

            lock (sync_root)
            {
                var policy = RetryPolicy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(_timeSpans, (ex, time) =>
                    {
                        _logger.Warn("TryConnect/WaitAndRetry", "DefaultRabbitMQPersistentConnection", ex.ToString());
                    }
                );

                policy.Execute(() =>
                {
                    _connection = _connectionFactory
                          .CreateConnection();
                });

                if (IsConnected)
                {
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;

                    _logger.Info("TryConnect", "DefaultRabbitMQPersistentConnection", $"RabbitMQ persistent connection acquired a connection {_connection.Endpoint.HostName} and is subscribed to failure events");

                    return true;
                }
                else
                {
                    _logger.Fatal("TryConnect", "DefaultRabbitMQPersistentConnection", "FATAL ERROR: RabbitMQ connections could not be created and opened");

                    return false;
                }
            }
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;

            _logger.Warn("OnConnectionBlocked", "DefaultRabbitMQPersistentConnection", "A RabbitMQ connection is shutdown. Trying to re-connect...");

            TryConnect();
        }

        void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            _logger.Warn("OnCallbackException", "DefaultRabbitMQPersistentConnection", "A RabbitMQ connection throw exception. Trying to re-connect...");

            TryConnect();
        }

        void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;

            _logger.Warn("OnCallbackException", "DefaultRabbitMQPersistentConnection", "A RabbitMQ connection is on shutdown. Trying to re-connect...");

            TryConnect();
        }
    }
}
