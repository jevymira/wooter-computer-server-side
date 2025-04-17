using Server.Services;

namespace Server;

public static class WootServiceExtensions
{
    public static async Task<WootService> GetAllPropertiesForFeedItems(this Task<WootService> task)
    {
        var service = await task;
        return await service.GetAllPropertiesForFeedItems();
    }
}
