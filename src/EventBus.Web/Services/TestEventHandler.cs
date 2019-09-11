using EventBus.Web.Models;
using Freamwork.EventBus;
using Freamwork.EventBus.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace EventBus.Web.Services
{
    public class TestEventHandler : IntegrationEventHandler<TestEvents>
    {
        public TestEventHandler() : base() { }


        public override Task Subscribe(TestEvents @event)
        {
            throw new Exception();
            return Task.Run(() => { });
        }
    }
}