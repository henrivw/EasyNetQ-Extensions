using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;
using System;
using System.Threading.Tasks;

namespace EasyNetQ
{
    public static class BusExtensions
    {
        public static void FuturePub<T>(this IBus bus, DateTime publishDate, T message) where T : class
        {
            FuturePubAsync(bus, publishDate, message).Wait();
        }

        public static Task FuturePubAsync<T>(this IBus bus, DateTime publishDate, T message) where T : class
        {
            // Preconditions.CheckNotNull(message, "message");

            var advancedBus = bus.Advanced;

            // If the date is not in future, publish the usual way
            if (publishDate.CompareTo(DateTime.Now) <= 0)
            {
                return bus.PublishAsync(message);
            }

            var conventions = advancedBus.Container.Resolve<IConventions>();
            var ScheduleMeType = typeof(ScheduleMe);
            var exchangeName = conventions.ExchangeNamingConvention(ScheduleMeType);
            var queueName = conventions.QueueNamingConvention(ScheduleMeType, string.Empty);

            advancedBus.ExchangeDeclare(exchangeName, ExchangeType.Topic);
            advancedBus.CreateAndBindExchangeAndQueue(exchangeName + "_Pending", queueName + "Pending", exchangeName);

            var typeNameSerializer = advancedBus.Container.Resolve<ITypeNameSerializer>();
            var serializer = advancedBus.Container.Resolve<ISerializer>();

            var typeName = typeNameSerializer.Serialize(typeof(T));
            var messageBody = serializer.MessageToBytes(message);

            return bus.PublishAsync(new ScheduleMe
            {
                WakeTime = publishDate,
                BindingKey = typeName,
                InnerMessage = messageBody
            });
        }

        private static void CreateAndBindExchangeAndQueue(this IAdvancedBus advancedBus, string exchangeName, string queueName, string deadLetterExchange)
        {
            var exchange = advancedBus.ExchangeDeclare(exchangeName, ExchangeType.Topic);
            var queue = advancedBus.QueueDeclare(queueName, deadLetterExchange: deadLetterExchange);
            advancedBus.Bind(exchange, queue, "#");
        }

    }
}
