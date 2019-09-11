using Freamwork.EventBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EventBus.Web.Util
{
    public class MyLog : ILogger
    {
        public void Debug(string source, string tranCode, object msg)
        {
      
        }

        public void Error(string source, string tranCode, string messsage, Exception ex)
        {
           
        }

        public void Fatal(string source, string tranCode, object msg)
        {
           
        }

        public void Info(string source, string tranCode, object msg)
        {
   
        }

        public void Warn(string source, string tranCode, object msg)
        {
       
        }
    }
}