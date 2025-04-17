using Model;
using Server.Dtos;
using Server.Services;

namespace Server
{
    public class WootWorkerService(
        ILogger<WootWorkerService> logger,
        IServiceScopeFactory scopeFactory) : BackgroundService
    {
        private readonly ILogger<WootWorkerService> _logger = logger;
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<WootComputersSourceContext>();
                var service = scope.ServiceProvider.GetRequiredService<WootService>();

                await service
                    .GetComputers() // .Load()
                    .GetAllPropertiesForFeedItems(); // optional .Transform()

                await service.SaveOffersAsync(); // requires .Load() and .Transform()
                await service.UpdateSoldOutOffersAsync(); // requires .Load() only
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1800000, stoppingToken);
            }
        }
    }
}
