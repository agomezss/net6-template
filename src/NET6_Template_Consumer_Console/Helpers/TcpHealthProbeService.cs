using NET6_Template.App;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NET6_Template.Helpers
{
    public sealed class TcpHealthProbeService : BackgroundService
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly TcpListener _listener;
        private readonly ILogger<TcpHealthProbeService> _logger;
        private readonly int _healthCheckLogSeconds;
        private readonly int _healthCheckPort;
        private readonly DateTime _startTime;

        public TcpHealthProbeService(HealthCheckService healthCheckService, ILogger<TcpHealthProbeService> logger)
        {
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            _logger = logger;
            _healthCheckLogSeconds = int.Parse(System.Environment.GetEnvironmentVariable("healthCheckLogSeconds") ?? "60");
            _healthCheckPort = int.Parse(System.Environment.GetEnvironmentVariable("healthCheckPort") ?? "5000");
            _listener = new TcpListener(IPAddress.Any, _healthCheckPort);
            _startTime = DateTime.Now;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Started health check service.");
            Console.WriteLine($"{DateTime.Now} - Method: {MethodBase.GetCurrentMethod()} - Started health check service.");

            await Task.Yield();
            _listener.Start();

            while (!stoppingToken.IsCancellationRequested && !AppLifeCycle.ShutdownReceived)
            {
                // Recruit all healthcheck status
                await UpdateHeartbeatAsync(stoppingToken);
                Thread.Sleep(TimeSpan.FromSeconds(_healthCheckLogSeconds));
            }

            _listener.Stop();
        }

        private async Task UpdateHeartbeatAsync(CancellationToken token)
        {
            try
            {
                var result =  await _healthCheckService.CheckHealthAsync(token);

                var isHealthy = (DateTime.Now - _startTime).Minutes <= 1 || result.Status != HealthStatus.Unhealthy;

                if (!isHealthy)
                {
                    _logger.LogCritical("Service is unhealthy!!!");
                }

                _listener.Start();

                while (_listener.Server.IsBound && _listener.Pending())
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "An error occurred while checking heartbeat.");
                Console.WriteLine($"{DateTime.Now} - Method: {MethodBase.GetCurrentMethod()} - An error occurred while checking heartbeat. Ex: {ex.Message}");
            }
        }
    }
}
