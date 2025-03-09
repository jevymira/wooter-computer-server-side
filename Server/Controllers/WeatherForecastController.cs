using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IConfiguration _config;

        public WeatherForecastController(
            ILogger<WeatherForecastController> logger,
            IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("offers/{id}")]
        public async Task<HttpResponseMessage> GetOffer(string id)
        {
            var uri = $"https://developer.woot.com/offers/{id}";

            HttpClient client = new HttpClient(); // FIXME: use HTTPClientFactory
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            // Secret Manager tool (development), see:
            // https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows
            client.DefaultRequestHeaders.Add("x-api-key", _config["Woot:DeveloperApiKey"]);
            
            HttpResponseMessage response = await client.GetAsync(uri).ConfigureAwait(false);

            // if (response.IsSuccessStatusCode)

            return response;
        }
    }
}
