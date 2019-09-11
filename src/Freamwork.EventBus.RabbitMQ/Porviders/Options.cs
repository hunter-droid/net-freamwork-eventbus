using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freamwork.EventBus.RabbitMQ.Porviders
{
    public class Options
    {
        public ConnectionFactory Factory { get; set; }

        public TimeSpan[] ConnectionTimeSpans { get; set; }

        public TimeSpan[] PublishTimeSpans { get; set; }


        public TimeSpan[] SubscribeTimeSpans { get; set; }

        public string QueueNames { get; set; }
        /// <summary>
        /// 后缀名
        /// </summary>
        public string PostFix { get; set; }
    }
}
