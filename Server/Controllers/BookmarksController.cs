using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Dtos;
using Server.Services;
using System.Security.Claims;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookmarksController : ControllerBase
    {
        private readonly BookmarkService _service;

        public BookmarksController(BookmarkService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets all bookmarks that belong the authenticated user;
        /// optionally, filter by the offer item (configuration) ID.
        /// </summary>
        /// <returns>The authenticated user's bookmarks.</returns>
        [Authorize]
        [HttpGet] // GET: api/Bookmarks
        public async Task<ActionResult<IEnumerable<BookmarkDto>>> GetBookmarks(
            [FromQuery] int? offerItemId)
        {
            string? id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id is null)
            {
                return NotFound();
            }

            var bookmarks = await _service.GetBookmarksByUserIdAsync(id, offerItemId);
            var bookmarkDtos = bookmarks.Select(b => new BookmarkDto()
            {
                Id = b.Id,
                UserId = b.UserId,
                ConfigurationId = b.ConfigurationId,
                Category = b.HardwareConfiguration.Offer.Category,
                Title = b.HardwareConfiguration.Offer.Title,
                Photo = b.HardwareConfiguration.Offer.Photo,
                MemoryCapacity = b.HardwareConfiguration.MemoryCapacity,
                StorageSize = b.HardwareConfiguration.StorageSize,
                Price = b.HardwareConfiguration.Price,
                IsSoldOut = b.HardwareConfiguration.Offer.IsSoldOut
            }).ToList();
            return Ok(bookmarkDtos);
        }

        /// <summary>
        /// Gets a bookmark based on its identifier.
        /// </summary>
        /// <param name="id">The bookmark identifier.</param>
        /// <returns>
        /// A bookmark DTO that contains hardware specification and offer details.
        /// </returns>
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<BookmarkDto>> GetBookmark(int id)
        {
            var bookmark =  await _service.GetBookmark(id);

            if (bookmark is null)
            {
                return NoContent();
            }

            var bookmarkDto = new BookmarkDto()
            {
                Id = bookmark.Id,
                UserId = bookmark.UserId,
                ConfigurationId = bookmark.ConfigurationId,
                Category = bookmark.HardwareConfiguration.Offer.Category,
                Title = bookmark.HardwareConfiguration.Offer.Title,
                Photo = bookmark.HardwareConfiguration.Offer.Photo,
                MemoryCapacity = bookmark.HardwareConfiguration.MemoryCapacity,
                StorageSize = bookmark.HardwareConfiguration.StorageSize,
                Price = bookmark.HardwareConfiguration.Price,
                IsSoldOut = bookmark.HardwareConfiguration.Offer.IsSoldOut
            };

            return Ok(bookmarkDto);
        }

        /// <summary>
        /// Posts a bookmark that references the authenticated user and
        /// the specified hardware configuration identifier of an offer.
        /// </summary>
        // To protect from overposting attacks,
        // see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost] // POST: api/Bookmarks
        public async Task<ActionResult<BookmarkDto>> PostBookmark(int offerItemId)
        {
            string? id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id is null)
            {
                return NotFound();
            }

            var bookmark = await _service.CreateBookmarkAsync(id, offerItemId);

            if (bookmark is null)
            {
                return Conflict("Offer already bookmarked for user.");
            }

            var bookmarkDto = new BookmarkDto()
            {
                Id = bookmark.Id,
                UserId = bookmark.UserId,
                ConfigurationId = bookmark.ConfigurationId,
                Category = bookmark.HardwareConfiguration.Offer.Category,
                Title = bookmark.HardwareConfiguration.Offer.Title,
                Photo = bookmark.HardwareConfiguration.Offer.Photo,
                MemoryCapacity = bookmark.HardwareConfiguration.MemoryCapacity,
                StorageSize = bookmark.HardwareConfiguration.StorageSize,
                Price = bookmark.HardwareConfiguration.Price,
                IsSoldOut = bookmark.HardwareConfiguration.Offer.IsSoldOut
            };

            return CreatedAtAction("GetBookmark", new { id = bookmarkDto.Id }, bookmarkDto);
        }

        /// <summary>
        /// Deletes the bookmark that corresponds to the currently authenticated user
        /// and the specified offer item identifer, if it exists.
        /// </summary>
        /// <param name="offerItemId">The offer item (configuration) identifier.</param>
        /// <returns>NoContent if successful.</returns>
        [Authorize]
        [HttpDelete] // DELETE: api/Bookmarks/5
        public async Task<IActionResult> DeleteBookmark(int offerItemId)
        {
            string? id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id is null)
            {
                return NotFound();
            }

            var bookmark = await _service.GetBookmarksByUserIdAsync(id, offerItemId);
            if (bookmark == null)
            {
                return NotFound();
            }

            await _service.DeleteBookmarkAsync(id, offerItemId);

            return NoContent();
        }
    }
}
