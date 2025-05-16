using Server.Services;
using Server.Dtos;

namespace Server.Tests
{
    public class OfferServiceTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;

        public OfferServiceTests(DatabaseFixture fixture)
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
            OfferItemDto item_existing = await service.GetOfferConfigurationAsync(1);
            OfferItemDto item_notExisting = await service.GetOfferConfigurationAsync(32767);

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
            var result = await service.GetOffersAsync(new GetOffersRequestDto() {});

            // Assert
            Assert.Equal(expected, result.Count());
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
            var result = await service.GetOffersAsync(new GetOffersRequestDto()
            { 
                Category = "Desktops"
            });

            // Assert
            Assert.Equal(3, result.Count());
            Assert.All(result, o => Assert.Equal("Desktops", o.Category));
        }

        /// <summary>
        /// Tests the GetOffers() method, set to filter for a single select memory capacity.
        /// Applies no filters for "category" or "storage."
        /// </summary>
        [Theory]
        [InlineData(16)]
        public async Task GetOffersByMemoryCapacityAsync(short memory)
        {
            // Arrange
            var service = new OfferService(_fixture.Context);

            // Act
            var result = await service.GetOffersAsync(new GetOffersRequestDto()
            {
                Memory = [memory]
            });

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, o => Assert.True(o.MemoryCapacity == 16));
        }

        /// <summary>
        /// Tests the GetOffers() method, set to filter for a single select storage size.
        /// Applies no filters for "category" or "memory."
        /// </summary>
        [Theory]
        [InlineData(256)]
        public async Task GetOffersByStorageStorageAsync(short storage)
        {
            // Arrange
            var service = new OfferService(_fixture.Context);

            // Act
            var result = await service.GetOffersAsync(new GetOffersRequestDto()
            {
                Storage = [storage]
            });

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, o => Assert.True(o.StorageSize == 256));
        }

        /// <summary>
        /// Tests method GetOffers() with a single filter value applied for "memory"
        /// and several filter values for "storage".
        /// </summary>
        [Theory]
        [InlineData(16, new short[] { 256, 512 })]
        public async Task GetOffersWith16GbMemoryAndAnyAmongStorageSelection(short memory, short[] storage)
        {
            // Arrange
            var service = new OfferService(_fixture.Context);

            // Act
            var result = await service.GetOffersAsync(new GetOffersRequestDto()
            {
                Memory = [memory],
                Storage = [.. storage]
            });

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, o => Assert.Equal("Desktops", o.Category));
            Assert.All(result, o => Assert.True(o.MemoryCapacity == 16));
            Assert.All(result, o => Assert.True(o.StorageSize == 256 || o.StorageSize == 512));
        }

        /// <summary>
        /// Tests method GetOffers() with all filters active.
        /// Filters "memory" and "storage" with single values.
        /// </summary>
        [Theory]
        [InlineData(16, 256)]
        public async Task GetDesktopsWith16GbMemoryAnd256GbStorage(short memory, short storage)
        {
            // Arrange
            var service = new OfferService(_fixture.Context);

            // Act
            var result = await service.GetOffersAsync(new GetOffersRequestDto()
            {
                Category = "Desktops",
                Memory = [memory],
                Storage = [storage]
            });

            // Assert
            Assert.All(result, o => Assert.Equal("Desktops", o.Category));
            Assert.All(result, o => Assert.True(o.MemoryCapacity == memory));
            Assert.All(result, o => Assert.True(o.StorageSize == storage));
        }
    }
}
