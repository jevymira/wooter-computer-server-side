using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Model;
using Server.Controllers;
using Server.Dtos;

namespace Server.Tests;

public class BookmarksController_Test : IDisposable
{
    private readonly WootComputersSourceContext _context;

    /// <summary>
    /// Shares setup without sharing object instances.
    /// </summary>
    /// <remarks>
    /// Adapted from https://xunit.net/docs/shared-context#constructor.
    /// </remarks>
    public BookmarksController_Test()
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
        var controller = new BookmarksController(_context);

        // Act
        IEnumerable<BookmarkDto> result = (await controller.GetBookmarks(userId)).Value;

        // Assert
        Assert.Equal(expected, result.Count());
        Assert.All(result, b => Assert.Equal(userId, b.UserId));
    }

    [Fact]
    public async Task GetBookmarksUserDoesNotExist()
    {
        // Arrange
        var controller = new BookmarksController(_context);

        // Act
        var result = (await controller
            .GetBookmarks("12345678-abcd-abcd-abcd-123456789qrs")).Value;

        // Assert
        Assert.Equal(0, result.Count());
    }

    [Theory]
    // Valid user, with seeded bookmarks.
    [InlineData("12345678-1234-1234-1234-123456789xyz", 1)]
    public async Task PostBookmarkValidUser(string userId, int offerItemId)
    {
        // Arrange
        var controller = new BookmarksController(_context);

        // Act
        await controller.PostBookmark(userId, offerItemId);

        // Assert
        Assert.Equal(2, _context.Bookmarks.Count());
    }

    [Fact]
    public async Task DeleteBookmarkExists()
    {
        // Arrange
        var controller = new BookmarksController(_context);

        // Act
        var result = (await controller.DeleteBookmark(1));

        // Assert
        var notFoundResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, _context.Bookmarks.Count());
        Assert.Equal(204, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task DeleteBookmarkDoesNotExist()
    {
        // Arrange
        var controller = new BookmarksController(_context);

        // Act
        var result = (await controller.DeleteBookmark(-1));

        // Assert
        var notFoundResult = Assert.IsType<NotFoundResult>(result);
        // No change.
        Assert.Equal(1, _context.Bookmarks.Count());
        Assert.Equal(404, notFoundResult.StatusCode);
    }
}
