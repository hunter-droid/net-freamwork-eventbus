using EventBus.Web.Models;
using EventBus.Web.Services;
using Freamwork.EventBus;
using Freamwork.EventBus.Providers;
using Freamwork.EventBus.RabbitMQ;
using Freamwork.EventBus.RabbitMQ.Porviders;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using Unity;
using Unity.Injection;
using Unity.WebApi;

namespace EventBus.Web.Util
{
    public class EventBus
    {
        public static void Register()
        {
            IUnityContainer container = new UnityContainer();

            container.RegisterSingleton<ILogger, MyLog>();//注入日志
                                                          //注入订阅者
            container.RegisterType<IIntegrationEventHandler<TestEvents>, TestEventHandler>();

            //启用EventBus
            container.UseRabbitMQ(new Options
            {
                ConnectionTimeSpans = new[] { TimeSpan.FromSeconds(10) },
                PublishTimeSpans = new[] { TimeSpan.FromSeconds(10) },
                SubscribeTimeSpans = new[] { TimeSpan.FromSeconds(10) },
                QueueNames = "Test",
                PostFix = "local",
                Factory = new ConnectionFactory()
                {
                    HostName = "192.168.121.205",
                    UserName = "admin",
                    Password = "admin"
                }
            }).UseEventBus();
            //开启订阅者
            Subscribe(container);
            DependencyResolver.SetResolver(new Unity.Mvc5.UnityDependencyResolver(container));
            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);
        }

        public static void Subscribe(IUnityContainer container)
        {
            var eventBus = container.Resolve<IEventBus>();
            eventBus.Subscribe<TestEvents, IIntegrationEventHandler<TestEvents>>();
        }
    }
}