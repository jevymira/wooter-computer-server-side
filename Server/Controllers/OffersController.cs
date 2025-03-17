using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Model;
using NuGet.Packaging;
using Server.Dtos;
using Server.Services;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OffersController(
        WootComputersSourceContext context,
        IConfiguration config,
        WootService wootService) : ControllerBase
    {
        private readonly WootComputersSourceContext _context = context;
        private readonly IConfiguration _config = config;
        private readonly WootService _wootService = wootService;

        // GET: api/Offers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Offer>>> GetOffers()
        {
            return await _context.Offers.ToListAsync();
        }

        // GET: api/Offers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Offer>> GetOffer(int id)
        {
            var offer = await _context.Offers.FindAsync(id);

            if (offer == null)
            {
                return NotFound();
            }

            return offer;
        }

        // PUT: api/Offers/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOffer(int id, Offer offer)
        {
            if (id != offer.Id)
            {
                return BadRequest();
            }

            _context.Entry(offer).State = EntityState.Modified;

            /*
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OfferExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            */

            return NoContent();
        }

        [HttpGet("load")]
        public async Task<IActionResult> LoadOffers()
        {
            return NoContent();

            /*
            var uri = $"https://developer.woot.com/feed/Computers";

            HttpClient client = new HttpClient(); // FIXME: use HTTPClientFactory
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            // Secret Manager tool (development), see:
            // https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows
            client.DefaultRequestHeaders.Add("x-api-key", _config["Woot:DeveloperApiKey"]);

            // HttpResponseMessage, see:
            // https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpresponsemessage?view=net-8.0
            try
            {
                using HttpResponseMessage response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                WootNamedFeedDto? feed = JsonSerializer.Deserialize<WootNamedFeedDto>(responseBody);
                IEnumerable<WootFeedItemDto>? query = feed.Items.Where(o =>
                    o.Categories.Contains("PC/Desktops") ||
                    o.Categories.Contains("PC/Laptops"));
                int i = query.Count();

                uri = $"https://developer.woot.com/getoffers";
                
                ICollection<WootOfferDto> offers = new List<WootOfferDto>();

                int j = 0;

                // iterate through in increments of 25 offers (Woot API POST request body has a 25 offer max.)
                while (j < i)
                {
                    HttpClient client2 = new HttpClient(); // FIXME: use HTTPClientFactory
                    client2.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client2.DefaultRequestHeaders.Add("x-api-key", _config["Woot:DeveloperApiKey"]);
                    // Woot schema requires array of IDs
                    var offerIncrement = query.Skip(j).Take(25);
                    List<Guid> ids = new List<Guid>();
                    foreach (WootFeedItemDto offer in offerIncrement)
                    {
                        ids.Add(offer.OfferId);
                    }
                    HttpContent content = new StringContent(JsonSerializer.Serialize(ids));

                    using HttpResponseMessage response2 = await client2.PostAsync(uri, content);
                    response2.EnsureSuccessStatusCode();
                    string responseBody2 = await response2.Content.ReadAsStringAsync();

                    IEnumerable<WootOfferDto> returned = JsonSerializer.Deserialize<List<WootOfferDto>>(responseBody2);
                    offers.AddRange(returned);

                    j += 25;
                }
                return Ok(offers);
            }
            catch (HttpRequestException e)
            {
                return BadRequest(e.Message);
            }
            */
        }

        // FIXME: REFACTOR
        [HttpPut("load/{id}")]
        public async Task<IActionResult> LoadOffer(Guid id)
        {
            var uri = $"https://developer.woot.com/offers/{id}";

            HttpClient client = new HttpClient(); // FIXME: use HTTPClientFactory
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            // Secret Manager tool (development), see:
            // https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows
            client.DefaultRequestHeaders.Add("x-api-key", _config["Woot:DeveloperApiKey"]);

            // HttpResponseMessage, see:
            // https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpresponsemessage?view=net-8.0
            try
            {
                using HttpResponseMessage response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                WootOfferDto? offerDto = JsonSerializer.Deserialize<WootOfferDto>(responseBody);

                Offer offer = new()
                {
                    WootId = id,
                    Category = "PLACEHOLDER",
                    Title = offerDto.Title,
                    Photo = offerDto.Photo,
                    IsSoldOut = offerDto.IsSoldOut,
                    Condition = "PLACEHOLDER",
                    Url = offerDto.Url,
                };


                // items is a Woot! catch-all; more specifically called "Models" for electronics (or "Configurations" on other retailers)
                foreach (WootOfferItemDto item in offerDto.Items)
                {
                    WootOfferItemAttributeDto? a = item.Attributes.Where(x => x.Key == "Model").FirstOrDefault();
                    string s = string.Empty;
                    if (a != null)
                    {
                        s = a.Value.ToString();
                    }

                    // regular expression to extract specifications
                    var regex = new Regex(@"([0-9]{1,2})GB\W+([0-9]{1,4})");
                    var match = regex.Match(s);

                    short storage = Int16.Parse(match.Groups[2].Value);
                    if (s.Contains("TB")) { // FIXME: perform more specific check
                        // 1000GB per 1TB
                        storage *= 1000;
                    }

                    // FIXME: remove Model. namespace prefix once renamed
                    offer.Configurations.Add(new Model.Configuration
                    {
                        WootId = item.Id,
                        Processor = string.Empty,
                        MemoryCapacity = Int16.Parse(match.Groups[1].Value),
                        StorageSize = storage,
                        Price = item.SalePrice
                    });
                }

                if (offer.Configurations.First().Processor == null ||
                    offer.Configurations.First().Processor == String.Empty)
                {
                    offer.Configurations.First().Processor = "SINGLE MODEL";
                };

                _context.Offers.Add(offer);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (HttpRequestException e)
            {
                return BadRequest(e.Message);
            }
        }

        // POST: api/Offers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Offer>> PostOffer(Offer offer)
        {
            _context.Offers.Add(offer);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetOffer", new { id = offer.Id }, offer);
        }

        // DELETE: api/Offers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOffer(int id)
        {
            var offer = await _context.Offers.FindAsync(id);
            if (offer == null)
            {
                return NotFound();
            }

            _context.Offers.Remove(offer);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OfferExists(Guid wootId)
        {
            return _context.Offers.Any(e => e.WootId == wootId);
        }
    }
}
