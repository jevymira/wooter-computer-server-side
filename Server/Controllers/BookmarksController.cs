using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Model;
using Server.Dtos;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookmarksController : ControllerBase
    {
        private readonly WootComputersSourceContext _context;

        public BookmarksController(WootComputersSourceContext context)
        {
            _context = context;
        }

        // GET: api/Bookmarks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookmarkDto>>> GetBookmarks(string userId)
        {
            return await _context.Bookmarks
                .Where(b => b.UserId == userId)
                .Select(b => new BookmarkDto()
                {
                    Id = b.Id,
                    UserId = b.UserId,
                    ConfigurationId = b.ConfigurationId,
                    Title = b.HardwareConfiguration.Offer.Title,
                    Photo = b.HardwareConfiguration.Offer.Photo,
                    MemoryCapacity = b.HardwareConfiguration.MemoryCapacity,
                    StorageSize = b.HardwareConfiguration.StorageSize,
                    Price = b.HardwareConfiguration.Price,
                    IsSoldOut = b.HardwareConfiguration.Offer.IsSoldOut
                })
                .ToListAsync();
        }

        // POST: api/Bookmarks
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult> PostBookmark(string userId, int offerItemId)
        {
            var bookmark = new Bookmark()
            {
                UserId = userId,
                ConfigurationId = offerItemId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookmarks.Add(bookmark);
            await _context.SaveChangesAsync();

            /* return CreatedAtAction("GetBookmark", new { id = bookmark.Id }, bookmark); */
            return NoContent();
        }

        // DELETE: api/Bookmarks/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBookmark(int id)
        {
            var bookmark = await _context.Bookmarks.FindAsync(id);
            if (bookmark == null)
            {
                return NotFound();
            }

            _context.Bookmarks.Remove(bookmark);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool BookmarkExists(int id)
        {
            return _context.Bookmarks.Any(e => e.Id == id);
        }
    }
}
