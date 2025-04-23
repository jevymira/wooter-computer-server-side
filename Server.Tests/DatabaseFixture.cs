using Microsoft.EntityFrameworkCore;
using Model;

namespace Server.Tests;

/// <remarks>
/// Adapted from xUnit documentation at
/// https://xunit.net/docs/shared-context#class-fixture.
/// </remarks>
public class DatabaseFixture : IDisposable
{
    public WootComputersSourceContext Context { get; set; }

    public DatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<WootComputersSourceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new WootComputersSourceContext(options);

        // In-stock, multi-configuration Desktop.
        context.Add(new Offer()
        {
            WootId = Guid.NewGuid(),
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

        context.Add(new Offer()
        {
            WootId = Guid.NewGuid(),
            Category = "Desktops",
            Title = "Dell Optiplex 7070",
            Photo = "https://d3gqasl9vmjfd8.cloudfront.net/8697eca7-d3bc-451b-9111-9086936d8ecf.png",
            IsSoldOut = true,
            Condition = "Refurbished",
            Url = "https://computers.woot.com/offers/dell-optiplex-7070-micro-desktop-mini-pc-5",
            Configurations = new[] { new HardwareConfiguration()
                    { MemoryCapacity = 16, StorageSize = 256 }
                }
        });

        context.Add(new Offer()
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

        context.SaveChanges();

        Context = context;
    }

    public void Dispose()
    {
        Context.Dispose();
        GC.SuppressFinalize(this);
    }
}
