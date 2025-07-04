﻿using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Model;
using NuGet.Packaging;
using Server.Dtos;
using Server.Services.Interfaces;
using System.Text.RegularExpressions;

namespace Server.Services;

public class WootService
{
    private readonly IWootClient _wootClient;
    private readonly WootComputersSourceContext _context;
    // Acceptable to maintain state because not invoked via HTTP.
    private readonly List<WootFeedItemDto> _feedItems;
    private readonly ICollection<WootOfferDto> _wootOffers;

    public WootService(
        IWootClient wootClient,
        WootComputersSourceContext context)
    {
        _wootClient = wootClient;
        _context = context;
        _feedItems = [];
        _wootOffers = new List<WootOfferDto>();
    }

    /// <summary>
    /// Retrieves and stores live minified Woot! offers under the
    /// "Computers/Desktops" and "Computers/Laptops" categories.
    /// </summary>
    public async Task<WootService> WithWootComputersFeedAsync() {
        // Retrieve the "Computers" feed.
        List<WootFeedItemDto> feed = await _wootClient.GetComputerFeedAsync();

        // Filter for Desktops and Laptops (and thus exclude e.g., Peripherals, Tablets).
        IEnumerable<WootFeedItemDto> items =
            feed.IsNullOrEmpty()
            ? new List<WootFeedItemDto>()
            : feed.Where(i =>
                i.Categories.Contains("PC/Desktops") ||
                i.Categories.Contains("PC/Laptops"));

        _feedItems.AddRange(items);

        return this;
    }

    /// <summary>
    /// Retrieves full Woot! offers from the Woot! API 
    /// based on the set of stored WootFeedItemDto IDs.
    /// </summary>
    public async Task<WootService> BuildWootOffersFromFeedAsync()
    {
        Dictionary<Guid,string> categoriesById = [];

        // Store category, for when after full offers (which exclude category info)
        // are retrieved from the Woot! API.
        foreach (var item in _feedItems)
        {
            string s = string.Empty;
            if (item.Categories.Contains("PC/Desktops"))
            {
                s = "Desktops";
            }
            else if (item.Categories.Contains("PC/Laptops"))
            {
                s = "Laptops";
            }
            categoriesById.Add(item.OfferId, s);
        }

        // Extract the Woot! OfferId of each FeedItem.
        List<Guid> ids = _feedItems.Select(items => items.OfferId).ToList();
        // Fetch the Woot! offers corresponding to the OfferIds.
        List<WootOfferDto> offers = await _wootClient.GetWootOffersAsync(ids);

        if (!offers.IsNullOrEmpty()) // Prevent AddRange() null parameter exception.
        {
            _wootOffers.AddRange(offers);
        }

        // Re-assign category.
        foreach (var offer in _wootOffers)
        {
            categoriesById.TryGetValue(offer.WootId, out string? category);
            offer.Category = (category is null) ? string.Empty : category;
        }

        return this;
    }

    /// <summary>
    /// Builds a collection of Offer objects (and their configurations) from the
    /// set of stored Woot! offers in the schema documented at
    /// https://developer.woot.com/#tocs_offer.
    /// Then, tracks them if not already, and persists them to the database.
    /// </summary>
    /// <returns>
    /// No result (as a terminal operation that persists to the database).
    /// </returns>
    public async Task AddNewOffersAsync()
    {
        List<Offer> offers = [];

        /*
        List<Offer> offers = _wootOffers
            .Select(o => new Offer() 
        {
            WootId = o.WootId,
            Category = o.Category,
            Title = o.Title,
            Photo = o.Photos.First().Url,
            IsSoldOut = o.IsSoldOut,
            Condition = String.Empty,
            Url = o.Url,
            Configurations = (o.FullTitle != null) // Guard against malformed offers.
                ? GetHardwareConfigurations(o)
                : []
        }).ToList();
        */

        foreach (var wootOffer in _wootOffers) {
            var offer = new Offer()
            {
                WootId = wootOffer.WootId,
                Category = wootOffer.Category,
                Title = wootOffer.Title,
                Photo = wootOffer.Photos.First().Url,
                IsSoldOut = wootOffer.IsSoldOut,
                Condition = String.Empty,
                Url = wootOffer.Url,
                Configurations = []
            };

            // Guard condition against malformed Woot! offers.
            if (wootOffer.FullTitle != null)
            {
                var configurations = GetHardwareConfigurations(wootOffer);

                offer.Configurations.AddRange(configurations);
            }

            offers.Add(offer);
        }

        foreach (var offer in offers)
        {
            var temp = _context.Offers.FirstOrDefault(o => o.WootId == offer.WootId);

            if (temp is null) // Track the offer if not already.
            {
                _context.Add(offer);
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates property `IsSoldOut` to true for existing offers that are either
    /// (A.) not included in the stored set of live WootFeedItemDtos from Woot! or
    /// (B.) included, but are marked as sold out.
    /// </summary>
    /// <returns>
    /// No result (as a terminal operation that persists to the database).
    /// </returns>
    /// <remarks>
    /// Minified offers from the Woot! API's GetNamedFeed endpoint, while live,
    /// are not necessarily still in stock.
    /// </remarks>
    public async Task UpdateSoldOutStatusAsync()
    {
        if (_feedItems.IsNullOrEmpty()) // Guard against faulty/empty responses.
        {
            return;
        }

        HashSet<Guid> inStockOfferIdSet = _feedItems // HashSet for lookup time
            .Where(o => !o.IsSoldOut) // Not all live offers necessarily in-stock.
            .Select(o => o.OfferId)
            .ToHashSet();

        // Compare tracked offers against the IDs of offers still in stock.
        foreach (var offer in _context.Offers)
        {
            // In the event that the offer receives more stock
            // or was mistakenly marked sold-out.
            if (inStockOfferIdSet.Contains(offer.WootId))
            {
                offer.IsSoldOut = false;
            }
            // Otherwise: offer is (A.) not in the feed of live offers
            // or (B.) included, but marked sold-out.
            else
            {
                offer.IsSoldOut = true;
            }
        }

        // Final guard against marking all offers sold-out.
        // (Assumes that such a case is erroneous.)
        if (!_context.Offers.Where(o => o.IsSoldOut == false).IsNullOrEmpty())
        {
            await _context.SaveChangesAsync();
        }
    }

    private List<HardwareConfiguration> GetHardwareConfigurations(WootOfferDto offer)
    {
        List<HardwareConfiguration> configurations = [];

        foreach (WootOfferItemDto item in offer.Items)
        {
            var (memory, storage) = GetHardwareSpecifications(item, offer);
            
            configurations.Add(new HardwareConfiguration
            {
                WootId = item.Id,
                Processor = string.Empty,
                MemoryCapacity = memory,
                StorageSize = storage,
                Price = item.SalePrice
            });
        }

        return configurations;
    }

    private (short memory, short storage) GetHardwareSpecifications(
        WootOfferItemDto item, WootOfferDto offer)
    {
        short memory = 0, storage = 0;

        // Find the Attribute object that contains the
        // hardware specifications of the Woot! item variant.
        WootOfferItemAttributeDto? attribute = item.Attributes
            .Where(a => a.Key == "Model").FirstOrDefault();

        // Prepare the specifications for regular expression(s).
        string specs = (attribute != null) ? attribute.Value : string.Empty;

        if ((offer.Items.Count > 1) && (specs.Contains("GB") || specs.Contains("TB")))
        {
            // First pass: match memory (GB) and storage (GB)
            Regex regex = new(@"([0-9]{1,2})GB.+([0-9]{3})GB");
            Match match = regex.Match(specs);

            // Second pass: match memory (GB), first
            if (!(match.Success))
            {
                regex = new Regex(@"([0-9]{1,2})GB");
                match = regex.Match(specs);
            }

            if (match.Groups[1].Success)
            {
                memory = Int16.Parse(match.Groups[1].Value);
            }

            if (match.Groups[2].Success)
            {
                storage = Int16.Parse(match.Groups[2].Value);
            }
            else // Second pass: then, match storage (GB/TB).
            {
                regex = new Regex(@"([1-9]{1,2})TB");
                match = regex.Match(specs);

                if (match.Success)
                {
                    storage = Int16.Parse(match.Groups[1].Value);
                    storage *= 1000;
                }

                // Match the offer-wide "FullTitle" property.
                regex = new Regex(@"([0-9]{3})GB");
                match = regex.Match(offer.FullTitle);

                if (match.Success)
                {
                    storage = Int16.Parse(match.Groups[1].Value);
                }

                // Check if storage is labeled in TB.
                if (offer.FullTitle.Contains("TB"))
                {
                    regex = new Regex(@"([1-9]{1,2})TB");
                    match = regex.Match(offer.FullTitle);

                    if (match.Success)
                    {
                        storage = Int16.Parse(match.Groups[1].Value);
                        storage *= 1000;
                    }
                }
            }
        }
        else // ((offer.Items.Count == 1) || !(specs.Contains("GB") || specs.Contains("TB"))
        {
            // Match memory (GB).
            Regex regex = new(@"([0-9]{1,2})GB");
            Match match = regex.Match(offer.FullTitle);

            if (match.Success)
            {
                memory = Int16.Parse(match.Groups[1].Value);
            }

            // Match storage (GB, e.g., 256GB, 512GB).
            regex = new Regex(@"([0-9]{3})GB");
            match = regex.Match(offer.FullTitle);

            if (match.Success)
            {
                storage = Int16.Parse(match.Groups[1].Value);
            }
            else
            {
                // Match storage (TB, e.g., 1TB, 2TB).
                regex = new Regex(@"([1-9]{1,2})TB");
                match = regex.Match(offer.FullTitle);

                if (match.Success)
                {
                    storage = Int16.Parse(match.Groups[1].Value);
                    storage *= 1000;
                }
            }
        }

        return (memory, storage);
    }
}
