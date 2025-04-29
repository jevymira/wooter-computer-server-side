using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Model;

namespace Server.Services;

public class OfferService
{
    private readonly WootComputersSourceContext _context;

    public OfferService(WootComputersSourceContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all offer hardware configurations from the database;
    /// optionally, filter by offer category and/or config memory/storage.
    /// </summary>
    /// <param name="category">Computer category (e.g., "desktop").</param>
    /// <param name="memory">System RAM in GB.</param>
    /// <param name="storage">System storage in GB.</param>
    public async Task<IEnumerable<Offer>> GetOffersAsync(
        string? category,
        List<short> memory,
        List<short> storage)
    {
        var filteredOffers = await _context.Offers
                .Where(o => !o.IsSoldOut)
                .Where(o => category.IsNullOrEmpty() || o.Category.Equals(category))
                .Include(o => o.Configurations)
                .Where(o => o.Configurations
                    .Any(c => (memory.IsNullOrEmpty() || memory.Contains(c.MemoryCapacity))
                    && (storage.IsNullOrEmpty() || storage.Contains(c.StorageSize))
                    && (c.MemoryCapacity != 0 && c.StorageSize != 0)))
                .Select (o => new Offer
                {
                    Id = o.Id,
                    WootId = o.WootId,
                    Category = o.Category,
                    Title = o.Title,
                    Photo = o.Photo,
                    IsSoldOut = o.IsSoldOut,
                    Condition = o.Condition,
                    Url = o.Url,
                    // AND across filter types; OR within filter types.
                    Configurations = (ICollection<HardwareConfiguration>) o.Configurations
                        .Where(c => ((memory.IsNullOrEmpty() || memory.Contains(c.MemoryCapacity))
                        && (storage.IsNullOrEmpty() || storage.Contains(c.StorageSize))
                        && (c.MemoryCapacity != 0 && c.StorageSize != 0))) // Malformed offers.
                })
                .ToListAsync();

        return filteredOffers;
    }

    /// <summary>
    /// Gets an offer hardware configuration from the database by its identifier.
    /// </summary>
    /// <param name="id">The hardware configuration ID.</param>
    /// <returns>A hardware configuration (i.e., "item") of an offer.</returns>
    public async Task<HardwareConfiguration?> GetOfferConfigurationAsync(int id)
    {
        return await _context.Configurations
            .Where(c => c.Id.Equals(id))
            .Include (c => c.Offer)
            .FirstOrDefaultAsync();
    }
}
