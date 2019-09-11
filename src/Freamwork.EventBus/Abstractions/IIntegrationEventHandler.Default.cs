using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;

namespace Freamwork.EventBus.Abstractions
{
    public abstract class IntegrationEventHandler<T> : IIntegrationEventHandler<T> where T : IntegrationEvent
    {
        private ILogger _logger;

        public IntegrationEventHandler()
        {
            _logger = Providers.Providers.Provider.Resolve<ILogger>();
        }

        public Task Handle(T @event)
        {
            _logger.Info("Handle/Event", "IntegrationEventHandler", @event);

            var result = Subscribe(@event);

            return result;
        }

        public abstract Task Subscribe(T @event);

    }
}
