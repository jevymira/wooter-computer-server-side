using Microsoft.EntityFrameworkCore;
using Model;
using Server.Controllers;

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
            context.Add(new Offer()
            {
                Id = 1,
                Category = "Desktops",
                Title = "Dell Optiplex 7070",
                IsSoldOut = false,
                Condition = "Refurbished",
                Url = "https://computers.woot.com/offers/dell-optiplex-7070-micro-desktop-mini-pc-5"
            });
            context.SaveChanges();

            var controller = new OffersController(context);
            Offer? offer_existing = null;
            Offer? offer_notExisting = null;

            // Act
            offer_existing = (await controller.GetOffer(1)).Value;
            offer_notExisting  = (await controller.GetOffer(2)).Value;

            // Assert
            Assert.NotNull(offer_existing);
            Assert.Null(offer_notExisting);
        }
    }
}
