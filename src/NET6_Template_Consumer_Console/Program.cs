using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NET6_Template.App;
using NET6_Template.Helpers;
using System;

namespace NET6_Template
{
    class Program
    {
        static void Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args)
               .ConfigureAppConfiguration((hostContext, builder) =>
               {
                   builder.AddEnvironmentVariables();

               }).Build();

            host.RunAsync();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
          Host.CreateDefaultBuilder(args)
               .ConfigureServices((hostContext, services) =>
               {
                   services.AddHostedService<AppLifetimeWorker>();
                   services.AddHostedService<TcpHealthProbeService>();
                   services.AddHostedService<Worker>();
                   services.AddHealthChecks().AddCheck<CustomHealthcheck>("healthcheck");
                   services.Configure<HostOptions>(opts =>
                   {
                       opts.ShutdownTimeout = TimeSpan.FromSeconds(15);
                       opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
                   });
               }).UseConsoleLifetime();
    }
}
