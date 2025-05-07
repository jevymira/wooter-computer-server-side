using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Model;
using Server.Dtos;

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
    /// optionally, filter by offer category and/or config memory/storage
    /// and sort by
    /// </summary>
    public async Task<IEnumerable<OfferItemDto>> GetOffersAsync(GetOffersRequestDto request)
    {
        var offers = await _context.Offers
            .Where(o => !o.IsSoldOut)
            .Where(o => request.Category.IsNullOrEmpty()
                || o.Category.Equals(request.Category))
            .Include(o => o.Configurations)
            .Where(o => o.Configurations
                .Any(c => (request.Memory.IsNullOrEmpty()
                    || request.Memory.Contains(c.MemoryCapacity))
                && (request.Storage.IsNullOrEmpty()
                    || request.Storage.Contains(c.StorageSize))
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
                    .Where(c => ((request.Memory.IsNullOrEmpty()
                        || request.Memory.Contains(c.MemoryCapacity))
                    && (request.Storage.IsNullOrEmpty()
                        || request.Storage.Contains(c.StorageSize))
                    && (c.MemoryCapacity != 0 && c.StorageSize != 0))) // Malformed offers.
            })
            .ToListAsync();

        List<OfferItemDto> items = [];

        // Convert to OfferItemDtos, separating out the configurations.
        foreach (var offer in offers)
        {
            foreach (var config in offer.Configurations)
            {
                items.Add(new OfferItemDto
                {
                    Id = config.Id,
                    Category = offer.Category,
                    Title = offer.Title,
                    Photo = offer.Photo,
                    MemoryCapacity = config.MemoryCapacity,
                    StorageSize = config.StorageSize,
                    Price = config.Price,
                    IsSoldOut = offer.IsSoldOut,
                    Url = offer.Url,
                });
            }
        }

        items = (request.SortOrder != "desc")
        ? items.OrderBy(i => i.Price).ToList()
        : items.OrderByDescending(i => i.Price).ToList();

        return items;
    }

    /// <summary>
    /// Gets an offer hardware configuration from the database by its identifier.
    /// </summary>
    /// <param name="id">The hardware configuration ID.</param>
    /// <returns>A hardware configuration (i.e., "item") of an offer.</returns>
    public async Task<OfferItemDto?> GetOfferConfigurationAsync(int id)
    {
        var item = await _context.Configurations
            .Where(c => c.Id.Equals(id))
            .Include (c => c.Offer)
            .FirstOrDefaultAsync();

        if (item == null)
        {
            return null;
        }
        else
        {
            return new OfferItemDto()
            {
                Id = item.Id,
                Category = item.Offer.Category,
                Title = item.Offer.Title,
                Photo = item.Offer.Photo,
                MemoryCapacity = item.MemoryCapacity,
                StorageSize = item.StorageSize,
                Price = item.Price,
                IsSoldOut = item.Offer.IsSoldOut,
                Url = item.Offer.Url,
            };
        }
    }
}
