using Server.Dtos;

namespace Server.Services.Interfaces;

public interface IWootClient
{
    /// <summary>
    /// Retrieve live minified Woot! offers under the "Computers" feed from the
    /// Woot! API GetNamedFeed endpoint at https://developer.woot.com/#getnamedfeed.
    /// </summary>
    /// <returns>
    /// The live minified Woot! offers under the "Computers" feed.
    /// </returns>
    public Task<List<WootFeedItemDto>> GetComputerFeedAsync();

    /// <summary>
    /// Retrieve whole Woot! offers from the Woot! API GetOffers endpoint,
    /// documented at https://developer.woot.com/#getoffers.
    /// </summary>
    /// <param name="ids">The IDs of the offers to retrieve.</param>
    /// <returns>The corresponding Woot! offers with all their properties.</returns>
    public Task<List<WootOfferDto>> GetWootOffersAsync(List<Guid> ids);
}
