using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Model;
using Server.Dtos;
using System.IdentityModel.Tokens.Jwt;

namespace Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AdminController(
         UserManager<WooterComputerUser> userManager,
         JwtHandler jwtHandler) : ControllerBase
{
    [HttpPost("Login")]
    public async Task<IActionResult> LoginAsync(LoginRequestDto loginRequest)
    {
        WooterComputerUser user = await userManager.FindByNameAsync(loginRequest.UserName);

        if (user == null)
        {
            return Unauthorized("Incorrect username or password.");
        }

        bool success = await userManager.CheckPasswordAsync(user, loginRequest.Password);

        if (!success)
        {
            return Unauthorized("Incorrect username or password.");
        }

        JwtSecurityToken token = await jwtHandler.GetTokenAsync(user);
        string tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponseDto
        {
            Success = true,
            Message = "Login successful.",
            Token = tokenString
        });
    }
}
