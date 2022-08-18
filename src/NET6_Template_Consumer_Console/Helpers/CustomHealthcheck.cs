using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace NET6_Template.Helpers
{
    internal class CustomHealthcheck : IHealthCheck
    {
        static Process process { get; set; }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            process = process ?? Process.GetCurrentProcess();
            var mem = process.WorkingSet64;
            var cpu = GetCpuUsageForProcess(process).GetAwaiter().GetResult();

            var status = cpu > 70 && cpu < 85 ? HealthStatus.Degraded :
                         cpu >= 85 ?
                         HealthStatus.Unhealthy :
                         HealthStatus.Healthy;

            Console.WriteLine($"Healthcheck - CPU: {cpu:N2} %. RAM: {mem / 1024.0:n3} K of working set. Total System Threads: {process.Threads?.Count} - {status}");

            var result = new HealthCheckResult(status, DateTime.Now.ToString());
            return Task.FromResult(result);
        }


        static async Task<double> GetCpuUsageForProcess(Process process)
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            await Task.Delay(500);

            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            return cpuUsageTotal * 100;
        }
    }
}
