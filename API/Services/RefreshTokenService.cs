using System.Security.Cryptography;
using System.Text;
using API.Configuration;
using Domain.Context;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace API.Services;

public interface IRefreshTokenService
{
    Task<AuthTokenPair> CreateTokenPairAsync(ApplicationUser user, IEnumerable<string> roles);
    Task<AuthTokenPair?> RefreshAsync(string refreshToken);
    Task RevokeAsync(string? refreshToken, string? accessTokenJti);
    Task<bool> IsAccessTokenRevokedAsync(string accessTokenJti);
}

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationContext _applicationContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtOptions _options;

    public RefreshTokenService(
        ApplicationContext applicationContext,
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> options)
    {
        _applicationContext = applicationContext;
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _options = options.Value;
    }

    public async Task<AuthTokenPair> CreateTokenPairAsync(ApplicationUser user, IEnumerable<string> roles)
    {
        var pair = await CreateTokenPairCoreAsync(user, roles);
        await _applicationContext.SaveChangesAsync();
        return pair.Pair;
    }

    public async Task<AuthTokenPair?> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var tokenHash = HashToken(refreshToken);
        var storedToken = await _applicationContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);

        if (storedToken is null ||
            storedToken.RevokedAtUtc.HasValue ||
            storedToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(storedToken.ApplicationUserId.ToString());
        if (user is null || !user.IsActive || user.IsDeleted)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var pairResult = await CreateTokenPairCoreAsync(user, roles);
        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = pairResult.RefreshTokenHash;
        storedToken.ModifiedOn = DateTime.UtcNow;

        await _applicationContext.SaveChangesAsync();
        return pairResult.Pair;
    }

    public async Task RevokeAsync(string? refreshToken, string? accessTokenJti)
    {
        var hashes = string.IsNullOrWhiteSpace(refreshToken)
            ? []
            : new[] { HashToken(refreshToken) };

        var tokens = await _applicationContext.RefreshTokens
            .Where(token =>
                (!string.IsNullOrWhiteSpace(accessTokenJti) && token.AccessTokenJti == accessTokenJti) ||
                hashes.Contains(token.TokenHash))
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.RevokedAtUtc ??= now;
            token.ModifiedOn = now;
        }

        if (tokens.Count > 0)
        {
            await _applicationContext.SaveChangesAsync();
        }
    }

    public Task<bool> IsAccessTokenRevokedAsync(string accessTokenJti)
    {
        return _applicationContext.RefreshTokens.AnyAsync(token =>
            token.AccessTokenJti == accessTokenJti &&
            (token.RevokedAtUtc.HasValue || token.ExpiresAtUtc <= DateTime.UtcNow || token.IsDeleted));
    }

    private async Task<(AuthTokenPair Pair, string RefreshTokenHash)> CreateTokenPairCoreAsync(
        ApplicationUser user,
        IEnumerable<string> roles)
    {
        var accessToken = await _jwtTokenService.CreateTokenAsync(user, roles);
        var refreshToken = CreateRawToken();
        var refreshTokenHash = HashToken(refreshToken);

        _applicationContext.RefreshTokens.Add(new RefreshToken
        {
            ApplicationUserId = user.Id,
            TokenHash = refreshTokenHash,
            AccessTokenJti = accessToken.Jti,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_options.RefreshTokenExpiresInDays)
        });

        return (
            new AuthTokenPair
            {
                UserId = user.Id,
                AccessToken = accessToken.AccessToken,
                ExpiresAtUtc = accessToken.ExpiresAtUtc,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(_options.RefreshTokenExpiresInDays)
            },
            refreshTokenHash);
    }

    private static string CreateRawToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}

public sealed class AuthTokenPair
{
    public Guid UserId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
}
