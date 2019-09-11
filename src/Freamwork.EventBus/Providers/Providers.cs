using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;

namespace Freamwork.EventBus.Providers
{
    public static class Providers
    {
        public static IUnityContainer Provider;

        public static IUnityContainer UseEventBus(this IUnityContainer container)
        {
            container.RegisterSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();//注入事件管理者
           
            Providers.Provider = container;//提供Provider容器

            return container;
        }
    }
}
