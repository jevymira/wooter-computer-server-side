using System.Net.Http.Headers;

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
    }
}
