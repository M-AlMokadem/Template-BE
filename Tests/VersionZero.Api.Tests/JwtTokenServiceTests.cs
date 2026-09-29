using API.Configuration;
using API.Services;
using Domain.Models;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace VersionZero.Api.Tests;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public async Task CreateTokenAsync_ContainsUserAndRoleClaims()
    {
        var service = new JwtTokenService(Options.Create(new JwtOptions
        {
            Key = "VersionZero.Test.Signing.Key.Must.Be.At.Least.32.Characters",
            Issuer = "VersionZero.Tests",
            Audience = "VersionZero.Tests.Client",
            ExpiresInMinutes = 15
        }));
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "user@example.com",
            Email = "user@example.com",
            FullName = "Test User"
        };

        var result = await service.CreateTokenAsync(user, ["User"]);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        Assert.Equal(user.Id.ToString(), token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Id.ToString(), token.Claims.Single(claim => claim.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("Test User", token.Claims.Single(claim => claim.Type == ClaimTypes.Name).Value);
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "User");
        Assert.Equal(token.Id, result.Jti);
    }
}
