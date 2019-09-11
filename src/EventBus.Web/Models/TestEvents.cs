using Freamwork.EventBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EventBus.Web.Models
{
    public class TestEvents: IntegrationEvent
    {
        public string Message { get; set; }
    }
}