﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Model;
using Server.Dtos;
using Server.Services;
using System;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OffersController(WootComputersSourceContext context) : ControllerBase
    {
        private readonly WootComputersSourceContext _context = context;

        // GET: api/Offers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OfferItemDto>>> GetOffers(
            [FromQuery] string? category,
            [FromQuery] List<short> memory,
            [FromQuery] List<short> storage)
        {
            var offers = await _context.Offers
                .Where(o => (category.IsNullOrEmpty() || o.Category.Equals(category))
                    && !o.IsSoldOut)
                .Include(c => c.Configurations
                    .Where(c => memory.IsNullOrEmpty() // empty query param
                        || (c.MemoryCapacity != 0 // filter out the few malformed configs
                            && memory.Contains(c.MemoryCapacity)))
                    .Where(c=> storage.IsNullOrEmpty() // AND across filter types
                        || (c.StorageSize != 0
                            && storage.Contains(c.StorageSize)))) // OR within filter types
                .ToListAsync();
            
            List<OfferItemDto> items = [];

            foreach (var offer in offers)
            {
                foreach (var config in offer.Configurations)
                {
                    items.Add(new OfferItemDto()
                    {
                        Id = config.Id,
                        Category = offer.Category,
                        Title = offer.Title,
                        Photo = offer.Photo,
                        MemoryCapacity = config.MemoryCapacity,
                        StorageSize = config.StorageSize,
                        Price = config.Price,
                        IsSoldOut = offer.IsSoldOut,
                        Url = offer.Url,
                    });
                }
            }

            return items;
        }

        // GET: api/Offers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OfferItemDto>> GetOffer(int id)
        {
            OfferItemDto? item = await _context.Configurations
                .Where(config => config.Id.Equals(id))
                .Select(config => new OfferItemDto
                {
                    Id = config.Id,
                    Category = config.Offer.Category,
                    Title = config.Offer.Title,
                    Photo = config.Offer.Photo,
                    MemoryCapacity = config.MemoryCapacity,
                    StorageSize = config.StorageSize,
                    Price = config.Price,
                    IsSoldOut = config.Offer.IsSoldOut,
                    Url = config.Offer.Url,
                })
                .SingleOrDefaultAsync();

            if (item == null)
            {
                return NotFound();
            }

            return item;
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
