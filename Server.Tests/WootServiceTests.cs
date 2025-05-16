using Microsoft.EntityFrameworkCore;
using Model;
using NSubstitute;
using Server.Dtos;
using Server.Services;
using Server.Services.Extensions;
using Server.Services.Interfaces;

namespace Server.Tests;

public class WootServiceTests : IDisposable
{
    private readonly WootComputersSourceContext _context;
    private readonly Guid _wootOfferId;

    /// <summary>
    /// Shares setup without sharing object instances.
    /// </summary>
    /// <remarks>
    /// Adapted from https://xunit.net/docs/shared-context#constructor.
    /// </remarks>
    public WootServiceTests()
    {
        var options = new DbContextOptionsBuilder<WootComputersSourceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WootComputersSourceContext(options);

        _wootOfferId = Guid.Parse("12345678-1234-1234-1234-123456789abc");

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

    /// <summary>
    /// Tests AddNewOffersAsync() for the case in which no offers are returned
    /// from the Woot! API.
    /// </summary>
    [Fact]
    public async Task AddNewOffersWithoutAnyOffers()
    {
        // Arrange
        var client = Substitute.For<IWootClient>();
        // Mock an (erroneous) empty return.
        client.GetComputerFeedAsync().Returns([]);
        // From the previous empty return, mock an empty parameter.
        client.GetWootOffersAsync([]).Returns([]);

        var service = new WootService(client, _context);

        // Act
        await service
            .WithWootComputersFeedAsync()   // Uses mocked GetComputerFeedAsync().
            .BuildWootOffersFromFeedAsync() // Uses mocked GetWootOffersAsync().
            .AddNewOffersAsync();           // Uses mocked DbContext.

        // Assert
        Assert.Equal(2, _context.Offers.Count());
    }

    public static IEnumerable<object[]> ExistingOfferData => new List<object[]> 
    {
        new object[]
        {
            new WootOfferDto()
            {
                WootId = Guid.Parse("12345678-1234-1234-1234-123456789abc"),
                Category = "Desktops",
                Title = "Dell Optiplex 7080",
                Photos = [new WootOfferPhotoDto() { Url = "placeholder" }],
                IsSoldOut = false,
                Condition = "Refurbished",
                Url = "placeholder",
                FullTitle = "placeholder"
            }
        }
    };

    /// <summary>
    /// Tests AddNewOffersAsync() for the case in which only already-tracked
    /// offers are returned from the Woot! API.
    /// </summary>
    [Theory]
    [MemberData(nameof(ExistingOfferData))]
    public async Task AddNewOffersButAlreadyTracked(WootOfferDto wootOffer)        
    {
        // Arrange
        var wootFeedItem = new WootFeedItemDto
        {
            // Matches a tracked offer.
            OfferId = wootOffer.WootId,
            Categories = ["PC/Desktops"],
            IsSoldOut = false
        };

        var client = Substitute.For<IWootClient>();
        client.GetComputerFeedAsync().Returns([wootFeedItem]);
        client.GetWootOffersAsync([wootFeedItem.OfferId]).Returns([wootOffer]);

        var service = new WootService(client, _context);

        // Act
        await service
            .WithWootComputersFeedAsync()   // Uses mocked GetComputerFeedAsync().
            .BuildWootOffersFromFeedAsync() // Uses mocked GetWootOffersAsync().
            .AddNewOffersAsync();           // Uses mocked DbContext.

        // Assert
        Assert.Equal(2, _context.Offers.Count());
    }

    public static IEnumerable<object[]> NewOfferData => new List<object[]>
    {
        new object[]
        {
            // Multi-configuration offer, specs a findable as the "Value" to a 
            // generalized "Key."
            new WootOfferDto()
            {
                WootId = Guid.NewGuid(),
                Category = "Desktops",
                Title = "Dell Precision 3440 SFF",
                Photos = [new WootOfferPhotoDto() { Url = "placeholder" }],
                IsSoldOut = false,
                Condition = "Refurbished",
                Url = "placeholder",
                FullTitle = "placeholder",
                Items = [
                    new WootOfferItemDto()
                    {
                        Attributes = [new WootOfferItemAttributeDto()
                        {
                            Key = "Model",
                            Value = "i7-9700 | 16GB | 256GB"
                        }],
                        Id = Guid.NewGuid(),
                        SalePrice = 299.99m
                    },
                    new WootOfferItemDto()
                    {
                        Attributes = [new WootOfferItemAttributeDto()
                        {
                            Key = "Model",
                            Value = "i7-9700 | 32GB | 512GB"
                        }],
                        Id = Guid.NewGuid(),
                        SalePrice = 349.99m
                    }
                ]
            }
        },
        new object[]
        {   // Multi-configuration offer: memory findable as "Value" to "Key,"
            // and storage findable in FullTitle.
            new WootOfferDto()
            {
                WootId = Guid.Parse("70807f86-b57e-4f69-8048-94425f3ee71e"),
                Category = "Desktops",
                Title = "Dell Optiplex 7080",
                Photos = [new WootOfferPhotoDto() { Url = "placeholder" }],
                IsSoldOut = false,
                Condition = "Refurbished",
                Url = "placeholder",
                FullTitle = "Dell OptiPlex 7080 Micro Form Factor Desktop PC, " +
                "Windows 11 Pro, Intel Core i5-10600T 6-Core 2.40GHz, " +
                "256GB SSD, (Your Choice of Memory)",
                Items = [
                    new WootOfferItemDto()
                    {
                        Attributes = [new WootOfferItemAttributeDto()
                        {
                            Key = "Model",
                            Value = "16GB"
                        }],
                        Id = Guid.NewGuid(),
                        SalePrice = 519.99m
                    },
                    new WootOfferItemDto()
                    {
                        Attributes = [new WootOfferItemAttributeDto()
                        {
                            Key = "Model",
                            Value = "32GB"
                        }],
                        Id = Guid.NewGuid(),
                        SalePrice = 559.99m
                    }
                ]
            }
        },
        new object[]
        {   // Single-configuration offer, specs only extractable from FullTitle.
            new WootOfferDto()
            {
                WootId = Guid.Parse("70807f86-b57e-4f69-8048-94425f3ee71e"),
                Category = "Desktops",
                Title = "Dell Optiplex 7080 Open Box",
                Photos = [new WootOfferPhotoDto() { Url = "placeholder" }],
                IsSoldOut = false,
                Condition = "Refurbished",
                Url = "placeholder",
                FullTitle = "Dell OptiPlex 7080 Micro Form Factor Desktop PC, " +
                "Windows 11 Pro, Intel Core i5-10600T 6-Core 2.40GHz, " +
                "32GB (2x16GB) DDR4-2666MHz Memory, Intel UHD Graphics 630, " +
                "256GB SSD, Dell OP7080532256MAR, Refurbished",
                Items = [
                    new WootOfferItemDto()
                    {
                        Attributes = [new WootOfferItemAttributeDto()
                        {
                            Key = "Model",
                            Value = "Dell OP7080532256MAR"
                        }],
                        Id = Guid.NewGuid(),
                        SalePrice = 378.29m
                    }
                ]
            }
        }
    };

    /// <summary>
    /// Tests AddNewOffersAsync() for the case in which untracked offers are
    /// returned from the Woot! API.
    /// </summary>
    [Theory]
    [MemberData(nameof(NewOfferData))]
    public async Task AddNewOffersSuccessful(WootOfferDto wootOffer)
    {
        var wootFeedItem = new WootFeedItemDto
        {
            // Does not match a tracked offer.
            OfferId = wootOffer.WootId,
            Categories = ["PC/Desktops"],
            IsSoldOut = false
        };

        // Arrange
        var client = Substitute.For<IWootClient>();
        client.GetComputerFeedAsync().Returns([wootFeedItem]);
        client.GetWootOffersAsync([]).ReturnsForAnyArgs([wootOffer]);

        var service = new WootService(client, _context);

        // Act
        await service
            .WithWootComputersFeedAsync()   // Uses mocked GetComputerFeedAsync().
            .BuildWootOffersFromFeedAsync() // Uses mocked GetWootOffersAsync().
            .AddNewOffersAsync();           // Uses mocked DbContext.

        // Assert
        // One more than the amount seeded.
        Assert.Equal(3, _context.Offers.Count());
        // Ensure regex extracts specifications; thus none with the default value 0.
        Assert.All(_context.Offers, o => Assert.All(o.Configurations,
            c => Assert.NotEqual(0, c.MemoryCapacity)));
        Assert.All(_context.Offers, o => Assert.All(o.Configurations,
            c => Assert.NotEqual(0, c.StorageSize)));
    }
}
