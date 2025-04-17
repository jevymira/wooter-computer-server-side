using Server.Services;

namespace Server;

/// <remarks>
/// For reference, see the forum posts:
/// https://stackoverflow.com/a/52739551 & https://stackoverflow.com/a/32112709.
/// </remarks>
public static class WootServiceExtensions
{
    /// <summary>
    /// Overloads and awaits an async method of WootService to allow its chaining.
    /// Retrieves full Woot! offers from the Woot! API 
    /// based on the set of stored WootFeedItemDto IDs.
    /// </summary>
    /// <param name="task">The extended Fluent API type.</param>
    /// <returns>The Fluent API object.</returns>
    public static async Task<WootService> BuildWootOffersFromFeedAsync(this Task<WootService> task)
    {
        var service = await task;
        return await service.BuildWootOffersFromFeedAsync();
    }

    /// <summary>
    /// Overloads and awaits an async method of WootService to allow its chaining.
    /// Builds a collection of Offer objects (and their configurations) from the
    /// set of stored Woot! offers in the schema documented at
    /// https://developer.woot.com/#tocs_offer.
    /// Then, tracks them if not already, and persists them to the database.
    /// </summary>
    /// <param name="task">The extended Fluent API type.</param>
    /// <returns>
    /// No result (as a terminal operation that persists to the database).
    /// </returns>
    public static async Task AddNewOffersAsync(this Task<WootService> task)
    {
        var service = await task;
        await service.AddNewOffersAsync();
    }

    /// <summary>
    /// Overloads and awaits an async method of WootService to allow its chaining.
    /// Updates property `IsSoldOut` to true for existing offers that are either
    /// (A.) not included in the stored set of live WootFeedItemDtos from Woot! or
    /// (B.) included, but are marked as sold out.
    /// </summary>
    /// <param name="task">The extended Fluent API type.</param>
    /// <returns>
    /// No result (as a terminal operation that persists to the database).
    /// </returns>
    public static async Task UpdateSoldOutStatusAsync(this Task<WootService> task)
    {
        var service = await task;
        await service.UpdateSoldOutStatusAsync();
    }
}
