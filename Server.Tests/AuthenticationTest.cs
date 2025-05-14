using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Model;
using NSubstitute;
using Server.Controllers;
using Server.Dtos;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Server.Tests;

public class AuthenticationTest
{
    public static IEnumerable<object[]> UserData()
    {
        yield return new object[]
        {
            new WooterComputerUser()
            {
                Id = "12345678-1234-1234-1234-123456789abc",
                UserName = "abc",
                Email = "abc@email",
                SecurityStamp = Guid.NewGuid().ToString(),
            }
        };
    }

    [Theory]
    [MemberData(nameof(UserData))]
    public async Task GetTokenWithUserId(WooterComputerUser user)
    {
        // Arrange
        var store = Substitute.For<IUserStore<WooterComputerUser>>();
        var userManager = Substitute.For<UserManager<WooterComputerUser>>(
            store, null, null, null, null, null, null, null, null
        );

        userManager.FindByNameAsync(user.UserName).Returns(Task.FromResult(user));
        userManager.CheckPasswordAsync(user, Arg.Any<string>()).Returns(callInfo =>
        {
            var password = callInfo.Arg<string>();
            return Task.FromResult(password == "mock-p4S$word");
        });

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,user.UserName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "testIssuer",
            audience: "testAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: null // Not needed.
        );

        var jwtHandler = Substitute.For<IJwtHandler>();
        jwtHandler.GetTokenAsync(user).Returns(Task.FromResult(token));

        var service = new AdminController(userManager, jwtHandler);

        // Act
        var result = await service.LoginAsync(
            new LoginRequestDto { UserName = user.UserName, Password = "mock-p4S$word" });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var loginResponse = Assert.IsType<LoginResponseDto>(okResult.Value);
        Assert.True(loginResponse.Success);
    }
}
