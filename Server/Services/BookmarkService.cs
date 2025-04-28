using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Model;

namespace Server.Services;

public class BookmarkService
{
    private readonly WootComputersSourceContext _context;

    public BookmarkService(WootComputersSourceContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all bookmarks from the database that belong the specified user ID;
    /// optionally, filter by the offer item (configuration) ID.
    /// </summary>
    public async Task<List<Bookmark>> GetBookmarksByUserIdAsync(string userId, int? offerItemId)
    {
        var bookmarks =  await _context.Bookmarks
            .Include(b => b.HardwareConfiguration)
            .ThenInclude(c => c.Offer)
            .Where(b => b.UserId == userId)
            .ToListAsync();

        if (offerItemId.HasValue)
        {
            bookmarks = bookmarks.Where(b => b.ConfigurationId == offerItemId).ToList();
        }

        return bookmarks;
    }

    /// <summary>
    /// Get a bookmark from the database based on its unique identifier.
    /// </summary>
    public async Task<Bookmark?> GetBookmark(int id)
    {
        return await _context.Bookmarks
            .Include(b => b.HardwareConfiguration)
            .ThenInclude(c => c.Offer)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    /// <summary>
    /// Creates a bookmark that references the authenticated user's ID
    /// and the specified hardware configuration identifier of an offer.
    /// </summary>
    public async Task<Bookmark?> CreateBookmarkAsync(string userId, int configId)
    {
        var existing = await _context.Bookmarks
            .Where(b => b.UserId.Equals(userId) && b.ConfigurationId.Equals(configId))
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            return null;
        }

        var bookmark = new Bookmark()
        {
            UserId = userId,
            ConfigurationId = configId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookmarks.Add(bookmark);
        await _context.SaveChangesAsync();

        // Explicit loading
        await _context.Entry(bookmark)
            .Reference(b => b.HardwareConfiguration)
            .LoadAsync();
        await _context.Entry(bookmark.HardwareConfiguration)
            .Reference(c => c.Offer)
            .LoadAsync();

        return bookmark;
    }

    /// <summary>
    /// Deletes a bookmark that references the authenticated user's ID
    /// and the specified hardware configuration identifier of an offer.
    /// </summary>
    public async Task DeleteBookmarkAsync(string userId, int configId)
    {
        var existing = await _context.Bookmarks
            .Where(b => b.UserId.Equals(userId) && b.ConfigurationId.Equals(configId))
            .FirstOrDefaultAsync();

        if (existing == null)
        {
            return;
        }

        _context.Bookmarks.Remove(existing);
        await _context.SaveChangesAsync();
    }
}
