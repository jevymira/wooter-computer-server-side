using Microsoft.EntityFrameworkCore;
using Model;
using Server.Services;

namespace Server.Tests;

public class BookmarkServiceTests : IDisposable
{
    private readonly WootComputersSourceContext _context;

    /// <summary>
    /// Shares setup without sharing object instances.
    /// </summary>
    /// <remarks>
    /// Adapted from https://xunit.net/docs/shared-context#constructor.
    /// </remarks>
    public BookmarkServiceTests()
    {
        var options = new DbContextOptionsBuilder<WootComputersSourceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WootComputersSourceContext(options);

        _context.Users.Add(new WooterComputerUser()
        {
            Id = "12345678-1234-1234-1234-123456789abc",
            UserName = "user",
            Email = "user@email",
            SecurityStamp = Guid.NewGuid().ToString(),
        });

        // User without any bookmarks.
        _context.Users.Add(new WooterComputerUser()
        {
            Id = "12345678-1234-1234-1234-123456789xyz",
            UserName = "none",
            Email = "none@email",
            SecurityStamp = Guid.NewGuid().ToString(),
        });

        _context.Add(new Offer()
        {
            Id = 1,
            WootId = Guid.NewGuid(),
            Category = "Desktops",
            Title = "Dell Optiplex 7080",
            Photo = "https://d3gqasl9vmjfd8.cloudfront.net/87ecd638-d90c-4006-ba40-87d01d6dd963.jpg",
            IsSoldOut = false,
            Condition = "Refurbished",
            Url = "https://computers.woot.com/offers/dell-optiplex-7080-micro-4?ref=w_cnt_lnd_cat_pc_5_72",
            Configurations = new[] {
                new HardwareConfiguration() { Id = 1, MemoryCapacity = 16, StorageSize = 256 },
                new HardwareConfiguration() { Id = 2, MemoryCapacity = 16, StorageSize = 512 },
                new HardwareConfiguration() { Id = 3, MemoryCapacity = 32, StorageSize = 1000 }
            }
        });

        _context.Add(new Bookmark()
        {
            UserId = "12345678-1234-1234-1234-123456789abc",
            ConfigurationId = 1,
            CreatedAt = DateTime.UtcNow,
        });

        _context.SaveChanges();
    } 

    public void Dispose()
    {
        _context.Dispose();
    }

    [Theory]
    // Valid user, with bookmarks.
    [InlineData("12345678-1234-1234-1234-123456789abc", 1)]
    // Valid user, without seeded bookmarks.
    [InlineData("12345678-1234-1234-1234-123456789xyz", 0)]
    public async Task GetBookmarksUsersExist(string userId, int expected)
    {
        // Arrange
        var service = new BookmarkService(_context);

        // Act
        var result = await service.GetBookmarksByUserIdAsync(userId, null);

        // Assert
        Assert.Equal(expected, result.Count());
        Assert.All(result, b => Assert.Equal(userId, b.UserId));
    }

    [Fact]
    public async Task GetBookmarksUserDoesNotExist()
    {
        // Arrange
        var service = new BookmarkService(_context);

        // Act
        var result = await service.GetBookmarksByUserIdAsync("12345678-abcd-abcd-abcd-123456789qrs", null);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    // Valid user, with seeded bookmarks.
    [InlineData("12345678-1234-1234-1234-123456789xyz", 1)]
    public async Task PostBookmarkValidUser(string userId, int offerItemId)
    {
        // Arrange
        var service = new BookmarkService(_context);

        // Act
        await service.CreateBookmarkAsync(userId, offerItemId);

        // Assert
        Assert.Equal(2, _context.Bookmarks.Count());
    }

    [Fact]
    public async Task DeleteBookmarkExists()
    {
        // Arrange
        var service = new BookmarkService(_context);

        // Act
        await service.DeleteBookmarkAsync("12345678-1234-1234-1234-123456789abc", 1);

        // Assert
        Assert.Equal(0, _context.Bookmarks.Count());
    }

    [Fact]
    public async Task DeleteBookmarkDoesNotExist()
    {
        // Arrange
        var service = new BookmarkService(_context);
        const int NON_EXISTENT_BOOKMARK_ID = -1;

        // Act
        await service.DeleteBookmarkAsync("12345678-1234-1234-1234-123456789abc", NON_EXISTENT_BOOKMARK_ID);

        // Assert
        // No change.
        Assert.Equal(1, _context.Bookmarks.Count());
    }
}
