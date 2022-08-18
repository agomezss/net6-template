using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NET6_Template.App
{
    public class AppLifetimeWorker : BackgroundService
    {
        public AppLifetimeWorker(IHostApplicationLifetime appLifetime)
        {
            appLifetime.ApplicationStopping.Register(OnStopping);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"*** Starting AppLifetime Worker");

            stoppingToken.Register(() => OnStopping());
            await Task.Delay(-1);
        }

        private void OnStopping()
        {
            if (AppLifeCycle.ShutdownReceived) return;

            Console.WriteLine("!!! SIGTERM received, sending graceful signal to process...");

            AppLifeCycle.Terminate();
        }
    }
}
