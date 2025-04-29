using Model;
using Server.Services;

namespace Server.Tests
{
    public class OfferService_Test : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;

        public OfferService_Test(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        /// <summary>
        /// Test the GetOfferConfigurationAsync() method.
        /// </summary>
        [Fact]
        public async Task GetOfferAsync()
        {
            // Arrange
            var service = new OfferService(_fixture.Context);

            // Act
            HardwareConfiguration item_existing = await service.GetOfferConfigurationAsync(1);
            HardwareConfiguration item_notExisting = await service.GetOfferConfigurationAsync(32767);

            // Assert
            Assert.NotNull(item_existing);
            Assert.Null(item_notExisting);
        }

        /// <summary>
        /// Tests the GetOffers() method, which filters out offers flagged isSoldOut.
        /// </summary>
        [Fact]
        public async Task GetAllOffersAsync()
        {
            // Arrange
            int expected = 4;

            var service = new OfferService(_fixture.Context);

            // Act
            var result = await service.GetOffersAsync(null, [], []);

            // Assert
            Assert.Equal(expected, result.SelectMany(o => o.Configurations).Count());
        }

        /// <summary>
        /// Tests the GetOffers() method, set to filter for category "Desktops."
        /// Applies no filters for "memory" or "storage."
        /// </summary>
        [Fact]
        public async Task GetOffersByCategoryAsync()
        {
            // Arrange
            var service = new OfferService(_fixture.Context);

            // Act
            var result = await service.GetOffersAsync("Desktops", [], []);

            // Assert
            Assert.Equal(3, result.SelectMany(o => o.Configurations).Count());
            Assert.All(result, o => Assert.Equal("Desktops", o.Category));
        }

        /// <summary>
        /// Tests the GetOffers() method, set to filter for a single select memory capacity.
        /// Applies no filters for "category" or "storage."
        /// </summary>
        [Fact]
        public async Task GetOffersByMemoryCapacityAsync()
        {
            // Arrange
            var service = new OfferService(_fixture.Context);

            // Act
            var result = await service.GetOffersAsync(null, [16], []);

            // Assert
            Assert.Equal(2, result.SelectMany(o => o.Configurations).Count());
            Assert.All(result, o => Assert.True(o.Configurations.All(c => c.MemoryCapacity == 16)));
        }

        /// <summary>
        /// Tests the GetOffers() method, set to filter for a single select storage size.
        /// Applies no filters for "category" or "memory."
        /// </summary>
        [Fact]
        public async Task GetOffersByStorageStorageAsync()
        {
            // Arrange
            var service = new OfferService(_fixture.Context);

            // Act
            var result = await service.GetOffersAsync(null, [], [256]);

            // Assert
            Assert.Equal(2, result.SelectMany(o => o.Configurations).Count());
            Assert.All(result, o => Assert.True(o.Configurations.All(c => c.StorageSize == 256)));
        }

        /// <summary>
        /// Tests method GetOffers() with a single filter value applied for "memory"
        /// and several filter values for "storage".
        /// </summary>
        [Fact]
        public async Task GetOffersWith16GbMemoryAndAnyAmongStorageSelection()
        {
            // Arrange
            var service = new OfferService(_fixture.Context);
            var storage = new List<short>() { 256, 512 };

            // Act
            var result = await service.GetOffersAsync(null, [16], storage);

            // Assert
            Assert.Equal(2, result.SelectMany(o => o.Configurations).Count());
            Assert.All(result, o => Assert.Equal("Desktops", o.Category));
            Assert.All(result, o => Assert.True(o.Configurations.All(c => c.MemoryCapacity == 16)));
            Assert.All(result, o => Assert.True(o.Configurations.All(c => c.StorageSize == 256 || c.StorageSize == 512)));
        }

        /// <summary>
        /// Tests method GetOffers() with all filters active.
        /// Filters "memory" and "storage" with single values.
        /// </summary>
        [Fact]
        public async Task GetDesktopsWith16GbMemoryAnd256GbStorage()
        {
            // Arrange
            var service = new OfferService(_fixture.Context);

            // Act
            var result = await service.GetOffersAsync("Desktops", [16], [256]);

            // Assert
            Assert.All(result, o => Assert.Equal("Desktops", o.Category));
            Assert.All(result, o => Assert.True(o.Configurations.All(c => c.MemoryCapacity == 16)));
            Assert.All(result, o => Assert.True(o.Configurations.All(c => c.StorageSize == 256)));
        }
    }
}
