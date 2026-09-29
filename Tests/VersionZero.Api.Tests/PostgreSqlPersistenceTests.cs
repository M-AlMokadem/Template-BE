using Domain.Context;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace VersionZero.Api.Tests;

public sealed class PostgreSqlPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private ApplicationContext _context = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _context = new ApplicationContext(options);
        await _context.Database.EnsureCreatedAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RefreshToken_CanBePersistedAndQueriedFromPostgreSql()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "integration@example.com",
            NormalizedUserName = "INTEGRATION@EXAMPLE.COM",
            Email = "integration@example.com",
            NormalizedEmail = "INTEGRATION@EXAMPLE.COM",
            FullName = "Integration User"
        };
        _context.ApplicationUsers.Add(user);
        await _context.SaveChangesAsync();

        var refreshToken = new RefreshToken
        {
            ApplicationUserId = user.Id,
            TokenHash = "integration-token-hash",
            AccessTokenJti = Guid.NewGuid().ToString("N"),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        };
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        var persisted = await _context.RefreshTokens
            .SingleAsync(token => token.TokenHash == refreshToken.TokenHash);

        Assert.Equal(user.Id, persisted.ApplicationUserId);
        Assert.Null(persisted.RevokedAtUtc);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
