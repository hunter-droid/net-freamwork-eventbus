using EventBus.Web.Models;
using Freamwork.EventBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Unity.Attributes;

namespace EventBus.Web.Controllers
{
    public class HomeController : Controller
    {
        [Dependency]
        public IEventBus EventBus { get; set; }

        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return View();
        }

        public ActionResult About()
        {
            TestEvents events = new TestEvents()
            {
                Message = "this is my first events"
            };

            EventBus.Publish(events);

            return Content("");
        }

     
    }
}
