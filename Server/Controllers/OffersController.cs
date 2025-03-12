using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Model;
using Server.Dtos;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OffersController(
        WootComputersSourceContext context,
        IConfiguration config) : ControllerBase
    {
        private readonly WootComputersSourceContext _context = context;
        private readonly IConfiguration _config = config;

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
                    Url = offerDto.Url
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
