using Microsoft.EntityFrameworkCore;
using Model;
using Server.Controllers;
using Server.Dtos;
using System.Configuration;

namespace Server.Tests
{
    public class OrdersController_Test
    {
        /// <summary>
        /// Test the GetOffer() method.
        /// </summary>
        [Fact]
        public async Task GetOffer()
        {
            // Arrange  
            var options = new DbContextOptionsBuilder<WootComputersSourceContext>()
                .UseInMemoryDatabase(databaseName: "WootComputers")
                .Options;
            using var context = new WootComputersSourceContext(options);

            var configuration = new HardwareConfiguration()
            {
                Id = 1,
                MemoryCapacity = 16,
                StorageSize = 256
            };

            context.Add(new Offer()
            {
                Id = 1,
                Category = "Desktops",
                Title = "Dell Optiplex 7070",
                Photo = "https://d3gqasl9vmjfd8.cloudfront.net/8697eca7-d3bc-451b-9111-9086936d8ecf.png",
                IsSoldOut = false,
                Condition = "Refurbished",
                Url = "https://computers.woot.com/offers/dell-optiplex-7070-micro-desktop-mini-pc-5",
                Configurations = new[] { configuration }
            });
            context.SaveChanges();

            var controller = new OffersController(context);
            OfferItemDto? offer_existing = null;
            OfferItemDto? offer_notExisting = null;

            // Act
            offer_existing = (await controller.GetOffer(1)).Value;
            offer_notExisting = (await controller.GetOffer(2)).Value;

            // Assert
            Assert.NotNull(offer_existing);
            Assert.Null(offer_notExisting);
        }
    }
}
