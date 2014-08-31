using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;
using System;

namespace EasyNetQ.FuturePub
{
    public class PublisherService : IPublisherService
    {
        private const string schedulerSubscriptionId = "FuturePub";
        private readonly IBus bus;
        private readonly IEasyNetQLogger log;
        private readonly Type ScheduleMeType = typeof(ScheduleMe);

        public PublisherService(IBus bus, IEasyNetQLogger log)
        {
            this.bus = bus;
            this.log = log;
        }

        public void Start()
        {
            log.DebugWrite("Starting PublisherService");

            bus.Subscribe<ScheduleMe>(schedulerSubscriptionId, Evaluate);
        }

        public void Stop()
        {
            log.DebugWrite("Stopping PublisherService");

            if (bus != null)
            {
                bus.Dispose();
            }
        }

        /// <summary>
        /// Handles a ScheduleMe that has expired from the Pending queue. If it is time to send the message, the message is published to the destination queue.
        /// If the time has not yet come, put the message back into the Pending queue with a new expiry time.
        /// </summary>
        /// <param name="message"></param>
        private void Evaluate(ScheduleMe message)
        {
            log.DebugWrite("Received message for publish at {0}", message.WakeTime);

            if (!bus.IsConnected) return;

            try
            {
                // If wake time is past, publish the message
                var timeToPublish = TimeToPublish(message);
                if (timeToPublish < 5000)
                {
                    Publish(message);
                }
                else
                {
                    // otherwise put it back on the pending queue
                    ReQueue(message, timeToPublish);
                }
            }
            catch (Exception ex)
            {
                log.ErrorWrite(ex);
            }
        }

        /// <summary>
        /// Returns a new expiry time span in milliseconds for the message.
        /// If the milliseconds exceeds Int32.MaxValue, returns MaxValue.
        /// If the expiry time is past, returns 0.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private int TimeToPublish(ScheduleMe message)
        {
            TimeSpan expirySpan = message.WakeTime.Subtract(DateTime.Now);
            double expiry = expirySpan.TotalMilliseconds;

            if (expiry > Int32.MaxValue) expiry = Int32.MaxValue;
            if (expiry < 0) expiry = 0;

            return Convert.ToInt32(expiry);            
        }

        /// <summary>
        /// Put the message back into the ScheduleMe_Pending queue 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="queueFor"></param>
        private void ReQueue(ScheduleMe message, int expiry)
        {
            try
            {
                var advancedBus = bus.Advanced;
                var conventions = advancedBus.Container.Resolve<IConventions>();

                var exchangeName = conventions.ExchangeNamingConvention(ScheduleMeType) + "_Pending";
                IExchange exchange = advancedBus.ExchangeDeclare(exchangeName, ExchangeType.Topic);
                IMessage<ScheduleMe> newMessage = new Message<ScheduleMe>(message);
                newMessage.Properties.Expiration = expiry.ToString();

                log.DebugWrite(string.Format("Requeuing message for {0}", FriendlyTimeSpan(expiry)));

                advancedBus.Publish(exchange, "#", false, false, newMessage);
            }
            catch (Exception ex)
            {
                log.ErrorWrite("Error in requeue \r\n{0}", ex);
            }
        }

        /// <summary>
        /// Send the message to the exchange it was ultimately destined for
        /// </summary>
        /// <param name="wrappedMessage"></param>
        private void Publish(ScheduleMe wrappedMessage)
        {
            try
            {
                var bindingKey = wrappedMessage.BindingKey;
                var message = wrappedMessage.InnerMessage;

                log.DebugWrite(string.Format(
                 "Publishing message to '{0}'", bindingKey));

                var exchange = bus.Advanced.ExchangeDeclare(bindingKey, ExchangeType.Topic);
                bus.Advanced.Publish(
                    exchange,
                    bindingKey,
                    false,
                    false,
                    new MessageProperties { Type = bindingKey },
                    message);
            }
            catch (Exception ex)
            {
                log.ErrorWrite("Error in publish \r\n{0}", ex);
            }
        }

        /// <summary>
        /// Format milliseconds to a readable timespan for log output
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        private string FriendlyTimeSpan(int milliseconds)
        {
            return milliseconds.ToString();
        }
    }
}