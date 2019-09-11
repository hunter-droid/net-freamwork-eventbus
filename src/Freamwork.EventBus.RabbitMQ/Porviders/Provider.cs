using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;
using Unity.Injection;

namespace Freamwork.EventBus.RabbitMQ.Porviders
{
    public static class Provider
    {
        public static IUnityContainer UseRabbitMQ(this IUnityContainer container, Options opt)
        {
            container.RegisterSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();//注入事件管理者

            container.RegisterSingleton<IRabbitMQPersistentConnection, DefaultRabbitMQPersistentConnection>(
                new InjectionConstructor(
                    opt.Factory,//RabbitMQ连接信息
                    opt.ConnectionTimeSpans //连接重试
            ));//注入RabbitMQ连接
            container.RegisterSingleton<IEventBus, EventBusRabbitMQ>(
                 new InjectionConstructor(
                 opt.PublishTimeSpans//发布重试
                 , opt.SubscribeTimeSpans//订阅重试
                 , opt.QueueNames//队列名
                 , opt.PostFix
                 ));//注入EventBus
            return container;
        }
    }
}