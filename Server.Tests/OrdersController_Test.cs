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
            int expected = 4;

            var controller = new OffersController(_fixture.Context);

            // Act
            var result = (await controller.GetOffers(null, [], [])).Value;

            // Assert
            Assert.Equal(expected, result.Count);
        }

        [Fact]
        public async Task GetOffersByCategoryAsync()
        {
            // Arrange
            var controller = new OffersController(_fixture.Context);

            // Act
            var result = (await controller.GetOffers("Desktops", [], [])).Value;

            // Assert
            Assert.Equal(3, result.Count);
            Assert.All(result, o => Assert.Equal("Desktops", o.Category));
        }

        [Fact]
        public async Task GetOffersByMemoryCapacityAsync()
        {
            // Arrange
            var controller = new OffersController(_fixture.Context);

            // Act
            var result = (await controller.GetOffers(null, [16], [])).Value;

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, o => Assert.Equal(16, o.MemoryCapacity));
        }

        [Fact]
        public async Task GetOffersByStorageStorageAsync()
        {
            // Arrange
            var controller = new OffersController(_fixture.Context);

            // Act
            var result = (await controller.GetOffers(null, [], [256])).Value;

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, o => Assert.Equal(256, o.StorageSize));
        }

        /// <summary>
        /// Tests method GetOffers() with all filters active.
        /// </summary>
        [Fact]
        public async Task GetDesktopsWith16GbMemoryAnd256GbStorage()
        {
            // Arrange
            var controller = new OffersController(_fixture.Context);

            // Act
            var result = (await controller.GetOffers("Desktops", [16], [256])).Value;

            // Assert
            Assert.All(result, o => Assert.Equal("Desktops", o.Category));
            Assert.All(result, o => Assert.Equal(16, o.MemoryCapacity));
            Assert.All(result, o => Assert.Equal(256, o.StorageSize));
        }
    }
}
