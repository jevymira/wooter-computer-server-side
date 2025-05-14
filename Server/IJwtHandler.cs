using Model;
using System.IdentityModel.Tokens.Jwt;

namespace Server;

public interface IJwtHandler
{
    public Task<JwtSecurityToken> GetTokenAsync(WooterComputerUser user);
}
