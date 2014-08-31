﻿using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyNetQ.FuturePub
{
    public class Logger : IEasyNetQLogger
    {
        private readonly ILog log;
        public Logger(ILog log)
        {
            this.log = log;
        }

        public void DebugWrite(string format, params object[] args)
        {
            log.DebugFormat(format, args);
        }

        public void InfoWrite(string format, params object[] args)
        {
            log.InfoFormat(format, args);
        }

        public void ErrorWrite(string format, params object[] args)
        {
            log.ErrorFormat(format, args);
        }

        public void ErrorWrite(Exception exception)
        {
            log.ErrorFormat(exception.ToString());
        }
    }
}
