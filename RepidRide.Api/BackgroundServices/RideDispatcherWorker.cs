using Microsoft.Extensions.Hosting; // Required for BackgroundService
using System.Threading;             // Required for CancellationToken
using System.Threading.Tasks;       // Required for Task

namespace RepidRide.Api.BackgroundServices
{
    // Inherit from BackgroundService to resolve the override error
    public class RideDispatcherWorker : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // 1. Check database for Pending RideRequests older than 15 seconds
                // 2. Cancel current driver ping
                // 3. Find next nearest driver via Spatial SQL Query
                // 4. Ping new driver via SignalR

                await Task.Delay(2000, stoppingToken); // Run every 2 seconds
            }
        }
    }
}
