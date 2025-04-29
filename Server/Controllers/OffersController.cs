using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Server.Dtos;
using Server.Services;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OffersController(OfferService service) : ControllerBase
    {
        private readonly OfferService _service = service;

        /// <summary>
        /// Gets all offer hardware configurations (i.e., offer "items");
        /// optionally, filter by offer category and/or config memory/storage.
        /// </summary>
        /// <param name="category">Computer category (e.g., "desktop").</param>
        /// <param name="memory">System RAM in GB.</param>
        /// <param name="storage">System storage in GB.</param>
        /// <returns></returns>
        [HttpGet] // GET: api/Offers
        public async Task<ActionResult<ICollection<OfferItemDto>>> GetOffers(
            [FromQuery] string? category,
            [FromQuery] List<short> memory,
            [FromQuery] List<short> storage)
        {
            List <OfferItemDto> items = [];
            IEnumerable<Model.Offer> offers = await _service
                .GetOffersAsync(category, memory, storage);

            foreach (var offer in offers)
            {
                foreach (var config in offer.Configurations)
                {
                    items.Add(new OfferItemDto
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

        /// <summary>
        /// Gets an offer hardware configuration (i.e., offer "item") by its identifier.
        /// </summary>
        /// <param name="id">The hardware configuration ID.</param>
        /// <returns>A hardware configuration (i.e., "item") of an offer.</returns>
        [HttpGet("{id}")] // GET: api/Offers/5
        public async Task<ActionResult<OfferItemDto>> GetOffer(int id)
        {
            var item = await _service.GetOfferConfigurationAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            return new OfferItemDto
            {
                Id = item.Id,
                Category = item.Offer.Category,
                Title = item.Offer.Title,
                Photo = item.Offer.Photo,
                MemoryCapacity = item.MemoryCapacity,
                StorageSize = item.StorageSize,
                Price = item.Price,
                IsSoldOut = item.Offer.IsSoldOut,
                Url = item.Offer.Url,
            };
        }
    }
}
