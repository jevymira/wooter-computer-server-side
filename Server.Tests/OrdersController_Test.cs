using Microsoft.EntityFrameworkCore;
using Model;
using Server.Controllers;
using Server.Dtos;

namespace Server.Tests
{
    public class OrdersController_Test : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;

        public OrdersController_Test(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        /// <summary>
        /// Test the GetOffer() method.
        /// </summary>
        [Fact]
        public async Task GetOfferAsync()
        {
            // Arrange
            var controller = new OffersController(_fixture.Context);

            // Act
            OfferItemDto? offer_existing = (await controller.GetOffer(1)).Value;
            OfferItemDto? offer_notExisting = (await controller.GetOffer(32767)).Value;

            // Assert
            Assert.NotNull(offer_existing);
            Assert.Null(offer_notExisting);
        }

        [Fact]
        public async Task GetAllOffersAsync()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<WootComputersSourceContext>()
                .EnableSensitiveDataLogging()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new WootComputersSourceContext(options);

            var expectedOffers = new List<Offer> {
                new Offer()
                {
                    WootId = Guid.NewGuid(),
                    Category = "Desktops",
                    Title = "Dell Optiplex 7070",
                    Photo = "https://d3gqasl9vmjfd8.cloudfront.net/8697eca7-d3bc-451b-9111-9086936d8ecf.png",
                    IsSoldOut = false,
                    Condition = "Refurbished",
                    Url = "https://computers.woot.com/offers/dell-optiplex-7070-micro-desktop-mini-pc-5",
                    Configurations = new[] { new HardwareConfiguration()
                        { MemoryCapacity = 16, StorageSize = 256 }
                    }
                },
                new Offer()
                {
                    WootId = Guid.NewGuid(),
                    Category = "Laptops",
                    Title = "Dell Latitude 7420",
                    Photo = "https://d3gqasl9vmjfd8.cloudfront.net/7819b57e-e82e-4656-a7e7-916f9606c0c7.jpg",
                    IsSoldOut = false,
                    Condition = "Refurbished",
                    Url = "https://computers.woot.com/offers/dell-latitude-7420-business-14-laptop-6",
                    Configurations = new[] { new HardwareConfiguration()
                        { MemoryCapacity = 32, StorageSize = 512}
                    }
                }
            };

            var controller = new OffersController(_fixture.Context);

            // Act
            var result = (await controller.GetOffers(null, [], [])).Value;

            // Assert
            Assert.Equal(expectedOffers.Count, result.Count);
        }

        [Fact]
        public async Task GetOffersByCategoryAsync()
        {
            // Arrange
            var controller = new OffersController(_fixture.Context);

            // Act
            var result = (await controller.GetOffers("Desktops", [], [])).Value;

            // Assert
            Assert.Equal(1, result.Count);
            Assert.All(result, o => Assert.Equal("Desktops", o.Category));
        }
    }
}
