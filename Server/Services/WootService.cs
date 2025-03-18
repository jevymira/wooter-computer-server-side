using NuGet.Packaging;
using Server.Dtos;
using System;
using System.Net.Http.Headers;
using System.Text.Json;
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
                offers.AddRange(JsonSerializer.Deserialize<IEnumerable<WootOfferDto>>(responseBody));
            }

            return offers;
        }
    }
}
