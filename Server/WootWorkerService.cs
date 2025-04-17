using Server.Services;

namespace Server
{
    public class WootWorkerService(
        IServiceScopeFactory scopeFactory,
        ILogger<WootWorkerService> logger
    ) : BackgroundService
    {

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<WootService>();

                await service
                    .GetComputers() // .Load()
                    .GetAllPropertiesForFeedItems(); // optional .Transform()

                await service.SaveOffersAsync(); // requires .Load() and .Transform()
                await service.UpdateSoldOutOffersAsync(); // requires .Load() only
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1800000, stoppingToken);
            }
        }
    }
}
