using Microsoft.EntityFrameworkCore;
using Model;
using NSubstitute;
using Server.Dtos;
using Server.Services;
using Server.Services.Interfaces;
using System;

namespace Server.Tests;

public class WootService_Test : IDisposable
{
    private readonly WootComputersSourceContext _context;
    private readonly Guid _wootOfferId;

    /// <summary>
    /// Shares setup without sharing object instances.
    /// </summary>
    /// <remarks>
    /// Adapted from https://xunit.net/docs/shared-context#constructor.
    /// </remarks>
    public WootService_Test()
    {
        var options = new DbContextOptionsBuilder<WootComputersSourceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WootComputersSourceContext(options);

        _wootOfferId = Guid.NewGuid();

        _context.Add(new Offer()
        {
            WootId = _wootOfferId,
            Category = "Desktops",
            Title = "Dell Optiplex 7080",
            Photo = "https://d3gqasl9vmjfd8.cloudfront.net/87ecd638-d90c-4006-ba40-87d01d6dd963.jpg",
            IsSoldOut = false,
            Condition = "Refurbished",
            Url = "https://computers.woot.com/offers/dell-optiplex-7080-micro-4?ref=w_cnt_lnd_cat_pc_5_72",
            Configurations = new[] {
                new HardwareConfiguration() { MemoryCapacity = 16, StorageSize = 256 },
                new HardwareConfiguration() { MemoryCapacity = 16, StorageSize = 512 },
                new HardwareConfiguration() { MemoryCapacity = 32, StorageSize = 1000 }
            }
        });

        _context.Add(new Offer()
        {
            WootId = Guid.NewGuid(),
            Category = "Laptops",
            Title = "Dell Latitude 7420",
            Photo = "https://d3gqasl9vmjfd8.cloudfront.net/7819b57e-e82e-4656-a7e7-916f9606c0c7.jpg",
            IsSoldOut = false,
            Condition = "Refurbished",
            Url = "https://computers.woot.com/offers/dell-latitude-7420-business-14-laptop-6",
            Configurations = new[] { new HardwareConfiguration()
                    { MemoryCapacity = 32, StorageSize = 256}
                }
        });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task UpdateSoldOutStatusNullOrEmpty()
    {
        // Arrange
        _context.SaveChanges();

        var client = Substitute.For<IWootClient>();
        // Mock an (erroneous) empty return.
        client.GetComputerFeedAsync().Returns([]);

        var service = new WootService(client, _context);

        // Act
        await service
            .WithWootComputersFeedAsync() // Uses mocked GetComputerFeedAsync().
            .UpdateSoldOutStatusAsync(); // Uses mocked DbContext.

        // Assert
        // A check is in place to guard against marking all offers sold-out,
        // in the event of some problem with the call by WootClient.
        Assert.Equal(2, _context.Offers.Where(o => o.IsSoldOut == false).Count());
    }

    /// <summary>
    /// Tests the UpdateSoldOutStatus() method for the case in which no tracked
    /// offer is included in the feed of live offers.
    /// </summary>
    [Fact]
    public async Task UpdateSoldOutStatusToTrueForAllOffers()
    {
        // Arrange
        var wootFeedItem = new WootFeedItemDto
        {
            OfferId = Guid.NewGuid(), // No match with entity already in database.
            Categories = ["PC/Desktops"],
            IsSoldOut = false,
        };

        var client = Substitute.For<IWootClient>();
        // Returns with a non-zero count of WootFeedItemDtos.
        client.GetComputerFeedAsync().Returns([wootFeedItem]);

        var service = new WootService(client, _context);

        // Act
        await service
            .WithWootComputersFeedAsync() // Uses mocked GetComputerFeedAsync().
            .UpdateSoldOutStatusAsync(); // Uses mocked DbContext.

        // Assert
        // All tracked offers are marked "sold-out."
        Assert.Equal(0, _context.Offers.Where(o => o.IsSoldOut == false).Count());
    }

    /// <summary>
    /// Tests updating the sold-out status of two offers, because one is
    /// included in the live feed but marked "sold-out," and the other is
    /// not included in the live feed and so no longer "live."
    /// </summary>
    [Fact]
    public async Task UpdateSoldOutStatusWhenIncludedButMarked()
    {
        // Arrange
        var wootFeedItem = new WootFeedItemDto
        {
            OfferId = _wootOfferId, // Matches a tracked offer.
            Categories = ["PC/Desktops"],
            IsSoldOut = true, // Distinct from previous test cases.
        };

        var client = Substitute.For<IWootClient>();
        client.GetComputerFeedAsync().Returns([wootFeedItem]);

        var service = new WootService(client, _context);

        // Act
        await service
            .WithWootComputersFeedAsync() // Uses mocked GetComputerFeedAsync().
            .UpdateSoldOutStatusAsync(); // Uses mocked DbContext.

        // Assert
        // Previous in-stock offer now "sold-out" after database update,
        // because it was included in the collection of live (but sold out) offers.
        // The other is now "sold-out", being not included in the live feed at all.
        Assert.Equal(0, _context.Offers.Where(o => o.IsSoldOut == false).Count());
    }
}
