using Model;
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

        public async Task<ICollection<Offer>> BuildOffers(ICollection<WootOfferDto> wootOffers)
        {
            ICollection<Offer> offers = new List<Offer>();

            foreach (var wootOffer in wootOffers) {
                Offer offer = new()
                {
                    WootId = wootOffer.WootId,
                    Category = wootOffer.Category,
                    Title = wootOffer.Title,
                    Photo = wootOffer.Photo,
                    IsSoldOut = wootOffer.IsSoldOut,
                    Condition = "PLACEHOLDER",
                    Url = wootOffer.Url,
                };

                foreach (WootOfferItemDto item in wootOffer.Items)
                {
                    WootOfferItemAttributeDto? a = item.Attributes.Where(x => x.Key == "Model").FirstOrDefault();
                    string s = string.Empty;
                    if (a != null)
                    {
                        s = a.Value.ToString();
                    }

                    // Regular expression to extract specifications.
                    var regex = new Regex(@"([0-9]{1,2})GB.+([0-9]{3})GB");
                    var match = regex.Match(s);

                    short memory = 0;
                    short storage = 0;

                    // TODO: refactor for readability & tests
                    // temp. solution for single object from the Woot! API w/out FullTitle
                    if (wootOffer.FullTitle != null)
                    {
                        // if offer has only one model/configuration
                        if (wootOffer.Items.Count == 1)
                        {
                            regex = new Regex(@"([0-9]{1,2})GB");
                            match = regex.Match(wootOffer.FullTitle);

                            if (match.Groups[1].Success)
                            {
                                memory = Int16.Parse(match.Groups[1].Value);
                            }

                            regex = new Regex(@"([0-9]{3})GB");
                            match = regex.Match(wootOffer.FullTitle);

                            if (match.Groups[1].Success)
                            {
                                storage = Int16.Parse(match.Groups[1].Value);
                            }
                            else
                            {
                                regex = new Regex(@"([1-9]{1,2})TB");
                                match = regex.Match(wootOffer.FullTitle);

                                if (match.Groups[1].Success)
                                {
                                    storage = Int16.Parse(match.Groups[1].Value);
                                    storage *= 1000;
                                }
                            }
                        }
                        else
                        {
                            if (!(match.Success))
                            {
                                regex = new Regex(@"([0-9]{1,2})GB");
                                match = regex.Match(s);
                            }

                            // assign memory
                            if (match.Groups[1].Success)
                            {
                                memory = Int16.Parse(match.Groups[1].Value);
                            }

                            // assign storage
                            if (match.Groups[2].Success)
                            {
                                storage = Int16.Parse(match.Groups[2].Value);
                            }
                            else
                            {
                                // otherwise, check the listing-wide "Specs" property
                                regex = new Regex(@"([0-9]{3})GB");
                                match = regex.Match(wootOffer.FullTitle);

                                if (match.Groups[1].Success)
                                {
                                    storage = Int16.Parse(match.Groups[1].Value);
                                }

                                if (wootOffer.FullTitle.Contains("TB"))
                                {
                                    regex = new Regex(@"([1-9]{1,2})TB");
                                    match = regex.Match(wootOffer.FullTitle);

                                    if (match.Groups[1].Success)
                                    {
                                        storage = Int16.Parse(match.Groups[1].Value);
                                        storage *= 1000;
                                    }
                                }
                            }
                        }

                        // FIXME: remove Model. namespace prefix once renamed
                        offer.Configurations.Add(new HardwareConfiguration
                        {
                            WootId = item.Id,
                            Processor = string.Empty,
                            MemoryCapacity = memory,
                            StorageSize = storage,
                            Price = item.SalePrice
                        });
                    }
                }

                /*
                if (offer.Configurations.First().Processor == null ||
                    offer.Configurations.First().Processor == String.Empty)
                {
                    offer.Configurations.First().Processor = "SINGLE MODEL";
                };
                */

                offers.Add(offer);
            }

            return offers;
        }
    }
}
