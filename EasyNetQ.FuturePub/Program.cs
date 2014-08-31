using log4net.Config;
using Topshelf;

namespace EasyNetQ.FuturePub
{
    public class Program
    {
        static void Main()
        {
            XmlConfigurator.Configure();

            HostFactory.Run(hostConfiguration =>
            {
                hostConfiguration.EnableServiceRecovery(serviceRecoveryConfiguration =>
                {
                    serviceRecoveryConfiguration.RestartService(delayInMinutes: 1); // On the first service failure, reset service after a minute
                    serviceRecoveryConfiguration.SetResetPeriod(days: 0); // Reset failure count after every failure
                });
                hostConfiguration.RunAsLocalSystem();
                hostConfiguration.SetDescription("EasyNetQ.FuturePub");
                hostConfiguration.SetDisplayName("EasyNetQ.FuturePub");
                hostConfiguration.SetServiceName("EasyNetQ.FuturePub");

                hostConfiguration.Service<IPublisherService>(serviceConfiguration =>
                {
                    serviceConfiguration.ConstructUsing(_ => PublisherServiceFactory.CreateScheduler());

                    serviceConfiguration.WhenStarted((service, _) =>
                    {
                        service.Start();
                        return true;
                    });
                    serviceConfiguration.WhenStopped((service, _) =>
                    {
                        service.Stop();
                        return true;
                    });
                });
            });
        }
    }
}
