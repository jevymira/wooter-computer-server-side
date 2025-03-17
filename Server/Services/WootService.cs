using Server.Dtos;
using System.Net.Http.Headers;
using System.Text.Json;

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
        /// and "Computers/Laptops" categories from the Woot! Developer API endpoint, 
        /// documented at https://developer.woot.com/#getnamedfeed
        /// </summary>
        /// <returns>
        /// The minified Woot! offers, filtered for the "Computers/Desktops"
        /// and "Computers/Laptops" categories.
        /// </returns>
        public async Task<IEnumerable<WootFeedItemDto>> GetComputers() {
            // Call the GetNamedFeed endpoint in Woot! Developer API.
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
    }
}
