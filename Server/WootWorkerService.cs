using Server.Services;
using Server.Services.Extensions;

namespace Server;

public class WootWorkerService(
    IServiceScopeFactory scopeFactory,
    ILogger<WootWorkerService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<WootService>();

                try
                {
                    // Setup and Intermediate operations.
                    await service
                        .WithWootComputersFeedAsync() // .Load()
                        .BuildWootOffersFromFeedAsync(); // optional .Transform()

                    // Terminal operations.
                    await service.AddNewOffersAsync(); // requires .Load() and .Transform()
                    await service.UpdateSoldOutStatusAsync(); // requires .Load() only
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in {worker}", GetType().Name);
                }
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            // Not a timer; it takes no account of job execution time.
            await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
        }
    }
}
