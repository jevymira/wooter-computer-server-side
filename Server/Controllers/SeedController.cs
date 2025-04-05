using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Model;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedController(
        WootComputersSourceContext context,
        IConfiguration config,
        UserManager<WooterComputerUser> manager) : ControllerBase
    {
        [HttpPost("Users")]
        public async Task ImportUsersAsync()
        {
            WooterComputerUser user = new()
            {
                // Secret Manager tool (development), see:
                // https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows
                UserName = config["SeedUser:UserName"],
                Email = config["SeedUser:Email"],
                SecurityStamp = Guid.NewGuid().ToString(),
            };

            await manager.CreateAsync(user, config["SeedUser:Password"]);
            await context.SaveChangesAsync();
        }
    }
}
