using System;
using log4net;

namespace EasyNetQ.FuturePub
{
    public static class PublisherServiceFactory
    {
        public static IPublisherService CreateScheduler()
        {
            var bus = RabbitHutch.CreateBus();
            var logger = new Logger(LogManager.GetLogger("EasyNetQ.FuturePub"));

            return new PublisherService(bus, logger);
        }
    }
}