using Server.Services;

namespace Server;

public static class WootServiceExtensions
{
    /// <summary>
    /// Overloads and awaits an async method of WootService to allow its chaining.
    /// </summary>
    /// <remarks>
    /// For reference, see the forum posts:
    /// https://stackoverflow.com/a/52739551 & https://stackoverflow.com/a/32112709.
    /// </remarks>
    /// <param name="task">The extended Fluent API type.</param>
    /// <returns>The Fluent API object.</returns>
    public static async Task<WootService> GetAllPropertiesForFeedItems(this Task<WootService> task)
    {
        var service = await task;
        return await service.GetAllPropertiesForFeedItems();
    }
}
