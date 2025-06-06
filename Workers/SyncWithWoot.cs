using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Server.Services;
using Server.Services.Extensions;

namespace Workers;

public class SyncWithWoot
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;

    public SyncWithWoot(IServiceScopeFactory scopeFactory, ILoggerFactory loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _logger = loggerFactory.CreateLogger<SyncWithWoot>();
    }

    /// <summary>
    /// Run the Azure Function to synchronize the database with the feed of live Woot! offers,
    /// at the start of each hour to account for Woot's daily request quota.
    /// </summary>
    [Function(nameof(SyncWithWoot))]
    public async Task Run([TimerTrigger("0 0 */1 * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {time}", DateTime.Now);
        
        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {time}", myTimer.ScheduleStatus.Next);
        }

        using (var scope = _scopeFactory.CreateScope())
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
                _logger.LogError(ex, "Error in {worker}", GetType().Name);
            }
        }
    }
}
