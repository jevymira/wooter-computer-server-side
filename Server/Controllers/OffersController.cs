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
            [FromQuery] GetOffersRequestDto request)
        {
            return Ok(await _service.GetOffersAsync(request));
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
            else
            {
                return Ok(item);
            }
        }
    }
}
