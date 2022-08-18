using log4net;
using Microsoft.Extensions.Hosting;
using NET6_Template.App;
using NET6_Template.Broker;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NET6_Template
{
    public class Worker : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            log4net.Config.BasicConfigurator.Configure();
            ILog log = LogManager.GetLogger(typeof(Program));

            log.Info($"*** Starting Broker Consumer");

            try
            {
                StartBrokerConsumer(log);
            }
            catch (Exception ex)
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    log.ErrorFormat("Error: {0}", ex.Message);
                    Environment.Exit(-1);
                }
                else
                {
                    while (!AppLifeCycle.ReadyToShutdown)
                    {
                        Task.Delay(1000).Wait();
                    }

                    Environment.Exit(0);
                }
            }

            return Task.CompletedTask;
        }

        static void StartBrokerConsumer(ILog log)
        {
            BrokerConnector.Init(ConfigBase.ReadSetting("QUEUE_ENDPOINT"));
            AppLifeCycle.Register(BrokerConnector.Close);
            new SampleConsumerBroker(log).ConsumeQueue(ConfigBase.ReadSetting("QUEUE_NAME"));
        }
    }
}
