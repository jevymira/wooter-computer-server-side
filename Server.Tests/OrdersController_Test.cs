using Microsoft.EntityFrameworkCore;
using Model;
using Server;
using Server.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Tests
{
    public class OrdersController_Test
    {
        /// <summary>
        /// Test the GetOffer() method.
        /// </summary>
        public async Task GetOffer()
        {
            // Arrange  
            var options = new DbContextOptionsBuilder<DbContext>()
                .UseInMemoryDatabase(databaseName: "WootComputers")
            .Options;
            using var context = new DbContext(options);
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
        }
    }
}
