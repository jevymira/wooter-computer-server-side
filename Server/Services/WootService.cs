﻿using Model;
using NuGet.Packaging;
using Server.Dtos;
using System;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Server.Services
{
    public class WootService
    {
        private readonly HttpClient _httpClient;

        // HttpClient configuration in constructor of Typed Client
        // rather than during registration in Program.cs, per:
        // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-8.0
        public WootService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://developer.woot.com/");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            // Secret Manager tool (development), see:
            // https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows
            _httpClient.DefaultRequestHeaders.Add("x-api-key", config["Woot:DeveloperApiKey"]);
        }

        /// <summary>
        /// Retrieve live minified Woot! offers in the "Computers/Desktops" 
        /// and "Computers/Laptops" categories from the Woot! API GetNamedFeed endpoint, 
        /// documented at https://developer.woot.com/#getnamedfeed
        /// </summary>
        /// <returns>
        /// The minified Woot! offers, filtered for the "Computers/Desktops"
        /// and "Computers/Laptops" categories.
        /// </returns>
        public async Task<IEnumerable<WootFeedItemDto>> GetComputers() {
            // Call the GetNamedFeed endpoint in the Woot! Developer API.
            using HttpResponseMessage response = await _httpClient.GetAsync("feed/Computers");

            // Deserialize the response body into WootMinifiedDtos.
            var responseBody = response.Content.ReadAsStream();
            WootNamedFeedDto feed = JsonSerializer.Deserialize<WootNamedFeedDto>(responseBody);

            // Filter for Desktops and Laptops (and thus exclude e.g., Peripherals, Tablets).
            IEnumerable<WootFeedItemDto> items = feed.Items.Where(o =>
                    o.Categories.Contains("PC/Desktops") ||
                    o.Categories.Contains("PC/Laptops"));

            return items;
        }

        /// <summary>
        /// Retrieve the requested offers with all their properties from the Woot! API
        /// GetOffers endpoint, documented at https://developer.woot.com/#getoffers
        /// </summary>
        /// <param name="items">The Woot! API Feed Items (minified offers).</param>
        /// <returns>The corresponding Woot! offers with all their properties.</returns>
        public async Task<ICollection<WootOfferDto>> GetAllPropertiesForFeedItems(
            IEnumerable<WootFeedItemDto> items)
        {
            Dictionary<Guid,string> categoriesById = [];

            // Store category, for when after full offers (which exclude category info)
            // are retrieved from the Woot! API.
            // FIXME: May be better implemented at the point of filtering,
            // see the GetComputers method.
            foreach (var item in items)
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

            ICollection<WootOfferDto> offers = new List<WootOfferDto>();

            // Iterate through in increments of 25 feed items per loop,
            // because the Woot! API's GetOffers endpoint enforces a 25-offer maximum.
            for (int i = 0; i < items.Count(); i += 25)
            {
                var increment = items.Skip(i).Take(25);
                // Extract the OfferId of each FeedItem.
                IEnumerable<Guid> ids = increment.Select(items => items.OfferId).ToList();

                // Assemble the body parameter for the request to the GetOffers Woot! API endpoint.
                HttpContent content = new StringContent(JsonSerializer.Serialize(ids));

                using HttpResponseMessage response = await _httpClient.PostAsync("getoffers", content);

                // Deserialize the response body into WootOfferDtos.
                var responseBody = response.Content.ReadAsStream();
                offers.AddRange(JsonSerializer.Deserialize<ICollection<WootOfferDto>>(responseBody));
            }

            // Re-assign category.
            foreach (var offer in offers)
            {
                categoriesById.TryGetValue(offer.WootId, out string category);
                offer.Category = category;
            }

            return offers;
        }

        /// <summary>
        /// Build a collection of Offer objects (and their configurations) from Woot!
        /// offers in schema documented at https://developer.woot.com/#tocs_offer.
        /// </summary>
        /// <param name="wootOffers">Offers in the Woot! schema.</param>
        /// <returns>Offer objects.</returns>
        public async Task<ICollection<Offer>> BuildOffers(ICollection<WootOfferDto> wootOffers)
        {
            ICollection<Offer> offers = new List<Offer>();

            foreach (var wootOffer in wootOffers) {
                Offer offer = new()
                {
                    WootId = wootOffer.WootId,
                    Category = wootOffer.Category,
                    Title = wootOffer.Title,
                    Photo = wootOffer.Photos.First().Url,
                    IsSoldOut = wootOffer.IsSoldOut,
                    Condition = "PLACEHOLDER",
                    Url = wootOffer.Url,
                };

                // Guard condition against malformed Woot! offers.
                if (wootOffer.FullTitle != null)
                {
                    var configurations = GetHardwareConfigurations(wootOffer);

                    offer.Configurations.AddRange(configurations);
                }

                offers.Add(offer);
            }

            return offers;
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
}
