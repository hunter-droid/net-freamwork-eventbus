using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Unity;

namespace Freamwork.EventBus.RabbitMQ
{
    public class EventBusRabbitMQ : IEventBus, IDisposable
    {
        private string BROKER_NAME;

        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly ILogger _logger;
        private readonly IEventBusSubscriptionsManager _subsManager;
        private readonly TimeSpan[] _pTimeSpans;
        private readonly TimeSpan[] _sTimeSpans;
        private IModel _consumerChannel;
        private string _queueName;

        public EventBusRabbitMQ(TimeSpan[] pTimeSpans, TimeSpan[] sTimeSpans, string queueName = null, string postfix = null)
        {
            if (string.IsNullOrEmpty(postfix))
            {
                BROKER_NAME = "freamwork_event_bus";
                _queueName = queueName;
            }
            else
            {
                BROKER_NAME = $"freamwork_event_bus_{postfix}";
                _queueName = $"{queueName }_{postfix}";
            }
            _persistentConnection = Providers.Providers.Provider.Resolve<IRabbitMQPersistentConnection>() ?? throw new ArgumentNullException("persistentConnection");
            _logger = Providers.Providers.Provider.Resolve<ILogger>() ?? throw new ArgumentNullException("logger");
            _subsManager = Providers.Providers.Provider.Resolve<IEventBusSubscriptionsManager>() ?? new InMemoryEventBusSubscriptionsManager();
            _consumerChannel = CreateConsumerChannel();
            _pTimeSpans = pTimeSpans;
            _sTimeSpans = sTimeSpans;
            _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;

        }

        private void SubsManager_OnEventRemoved(object sender, string eventName)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueUnbind(queue: _queueName,
                    exchange: BROKER_NAME,
                    routingKey: eventName, arguments: null);

                if (_subsManager.IsEmpty)
                {
                    _queueName = string.Empty;
                    _consumerChannel.Close();
                }
            }
        }

        public void Publish(IntegrationEvent @event)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }
            var policy = RetryPolicy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_pTimeSpans, (ex, time) =>
                {
                    _logger.Error("Publish/RetryPolicy", "EventBusRabbitMQ", $"time:{time}", ex);
                });
            using (var channel = _persistentConnection.CreateModel())
            {
                var eventName = @event.GetType()
                    .Name;

                channel.ExchangeDeclare(exchange: BROKER_NAME,
                                    type: "direct");

                var message = JsonConvert.SerializeObject(@event);
                var body = Encoding.UTF8.GetBytes(message);

                policy.Execute(() =>
                {
                    var properties = channel.CreateBasicProperties();
                    properties.DeliveryMode = 2; // persistent

                    channel.BasicPublish(exchange: BROKER_NAME,
                                     routingKey: eventName,
                                     mandatory: true,
                                     basicProperties: properties,
                                     body: body);
                });
            }
        }

        public void SubscribeDynamic<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            DoInternalSubscription(eventName);
            _subsManager.AddDynamicSubscription<TH>(eventName);
        }

        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = _subsManager.GetEventKey<T>();
            DoInternalSubscription(eventName);
            _subsManager.AddSubscription<T, TH>();
        }

        private void DoInternalSubscription(string eventName)
        {
            var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
            if (!containsKey)
            {
                if (!_persistentConnection.IsConnected)
                {
                    _persistentConnection.TryConnect();
                }
                using (var channel = _persistentConnection.CreateModel())
                {
                    IDictionary<string, object> args = new Dictionary<string, object>();
                    args.Add(new KeyValuePair<string, object>("x-dead-letter-routing-key", eventName));
                    channel.QueueBind(queue: _queueName,
                                      exchange: BROKER_NAME,
                                      routingKey: eventName, arguments: args);

                    channel.QueueBind(queue: $"dlx_{_queueName}",
                                     exchange: $"dlx_{BROKER_NAME}",
                                     routingKey: eventName);
                }
            }
        }

        public void Unsubscribe<T, TH>()
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationEvent
        {
            _subsManager.RemoveSubscription<T, TH>();
        }

        public void UnsubscribeDynamic<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            _subsManager.RemoveDynamicSubscription<TH>(eventName);
        }

        #region 释放资源 法师 2018年8月17日13:52:59
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_consumerChannel != null)
            {
                _consumerChannel.Dispose();
            }
            _subsManager.Clear();
        }
        #endregion

        private IModel CreateConsumerChannel()
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            var channel = _persistentConnection.CreateModel();

            channel.ExchangeDeclare(exchange: BROKER_NAME,
                                 type: "direct");
            channel.ExchangeDeclare(exchange: $"dlx_{BROKER_NAME}", type: "direct");
            IDictionary<string, object> args = new Dictionary<string, object>();
            args.Add(new KeyValuePair<string, object>("x-dead-letter-exchange", $"dlx_{BROKER_NAME}"));

            channel.QueueDeclare(queue: _queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: args);
            channel.QueueDeclare(queue: $"dlx_{_queueName}",
                      durable: true,
                      exclusive: false,
                      autoDelete: false,
                      arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var eventName = ea.RoutingKey;
                    var message = Encoding.UTF8.GetString(ea.Body);
                    var policy = RetryPolicy.Handle<Exception>()
                     .WaitAndRetryAsync(_pTimeSpans, (ex, time, context) =>
                     {
                         _logger.Error("Subscribe/RetryPolicy", "EventBusRabbitMQ", $"message:{ea.Body}--count:{context.Count}--time:{time}", ex);
                     });
                    await policy.ExecuteAsync(async () =>
                    {
                        await ProcessEvent(eventName, message);
                    });
                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.Error("Subscribe/RetryPolicy", "EventBusRabbitMQ", $"message:{ea.Body}", ex);
                    channel.BasicReject(ea.DeliveryTag, false);
                }
            };

            channel.BasicConsume(queue: _queueName,
                                 noAck: false,
                                 consumer: consumer);

            channel.CallbackException += (sender, ea) =>
            {
                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel();
            };

            return channel;
        }

        #region 处理事件 法师 2018年8月17日13:48:27
        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="eventName">事件名</param>
        /// <param name="message">消息</param>
        /// <returns></returns>
        private async Task ProcessEvent(string eventName, string message)
        {
            if (_subsManager.HasSubscriptionsForEvent(eventName))
            {

                var subscriptions = _subsManager.GetHandlersForEvent(eventName);
                foreach (var subscription in subscriptions)
                {
                    if (subscription.IsDynamic)
                    {
                        var handler = Providers.Providers.Provider.Resolve(subscription.HandlerType) as IDynamicIntegrationEventHandler;
                        dynamic eventData = JObject.Parse(message);
                        await handler.Handle(eventData);
                    }
                    else
                    {
                        var eventType = _subsManager.GetEventTypeByName(eventName);
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                        var handler = Providers.Providers.Provider.Resolve(subscription.HandlerType);
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                        await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                    }
                }
            }
        }
        #endregion
    }
}
