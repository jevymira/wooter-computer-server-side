using Server.Dtos;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Server.Services;

/// <summary>
/// Encapsulates HTTP requests to/responses from the Woot! Developer API,
/// documented at https://developer.woot.com/#woot-web-developer-api.
/// </summary>
/// <remarks>
/// Hence the "Client" naming scheme, aligned with HttpClient.
/// </remarks>
public class WootClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WootClient> _logger;

    public WootClient(
        IConfiguration config,
        HttpClient httpClient,
        ILogger<WootClient> logger)
    {
        // HttpClient configuration in constructor of Typed Client
        // rather than during registration in Program.cs, per:
        // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-8.0
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://developer.woot.com/");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        // Secret Manager tool (development), see:
        // https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows
        _httpClient.DefaultRequestHeaders.Add("x-api-key", config["Woot:DeveloperApiKey"]);
        _logger = logger;
    }
    /// <summary>
    /// Retrieve live minified Woot! offers under the "Computers" feed from the
    /// Woot! API GetNamedFeed endpoint at https://developer.woot.com/#getnamedfeed.
    /// </summary>
    /// <returns>
    /// The live minified Woot! offers under the "Computers" feed.
    /// </returns>
    public async Task<List<WootFeedItemDto>> GetComputerFeedAsync()
    {
        // Call the GetNamedFeed endpoint in the Woot! Developer API.
        using HttpResponseMessage response = await _httpClient.GetAsync("feed/Computers");
        Stream responseBody = response.Content.ReadAsStream();

        try
        {
            WootNamedFeedDto? feed = JsonSerializer.Deserialize<WootNamedFeedDto>(responseBody);
            return (feed is null) ? [] : feed.Items;
        }
        catch (JsonException)
        {
            _logger.LogInformation(response.Content.ToString());
            return [];
        }
    }

    /// <summary>
    /// Retrieve whole Woot! offers from the Woot! API GetOffers endpoint,
    /// documented at https://developer.woot.com/#getoffers
    /// </summary>
    /// <param name="ids">The IDs of the offers to retrieve.</param>
    /// <returns>The corresponding Woot! offers with all their properties.</returns>
    public async Task<List<WootOfferDto>> GetWootOffersAsync(List<Guid> ids)
    {
        List<WootOfferDto> offers = [];

        // Iterate through in increments of 25 ids per loop,
        // because the Woot! API's GetOffers endpoint enforces a 25-offer maximum.
        for (int i = 0; i < ids.Count; i += 25)
        {
            IEnumerable<Guid> idBatch = ids.Skip(i).Take(25);
            // Assemble the body parameter for the request to the GetOffers endpoint.
            HttpContent content = new StringContent(JsonSerializer.Serialize(idBatch));

            // Call the GetOffers endpoint in the Woot! Developer API.
            using HttpResponseMessage response = await _httpClient.PostAsync("getoffers", content);
            Stream responseBody = response.Content.ReadAsStream();

            try
            {
                var offerBatch = JsonSerializer.Deserialize<List<WootOfferDto>>(responseBody) ?? [];
                offers.AddRange(offerBatch);
            }
            catch (JsonException)
            {
                _logger.LogInformation(response.Content.ToString());
            }
        }

        return offers;
    }
}
