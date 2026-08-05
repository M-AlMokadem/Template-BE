using Domain.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API.Services;

public interface IJwtTokenService
{
	Task<AuthTokenResult> CreateTokenAsync(ApplicationUser user, IEnumerable<string> roles);
}

public sealed class JwtTokenService : IJwtTokenService
{
	private readonly IConfiguration configuration;

	public JwtTokenService(IConfiguration configuration)
	{
		this.configuration = configuration;
	}

	public Task<AuthTokenResult> CreateTokenAsync(ApplicationUser user, IEnumerable<string> roles)
	{
		var issuer = configuration["Jwt:Issuer"] ?? "VersionZero.API";
		var audience = configuration["Jwt:Audience"] ?? "VersionZero.Client";
		var key = configuration["Jwt:Key"] ?? "VersionZero.Dev.Secret.Key.For.Jwt.Token.Signing.2026";
		var expiresInMinutes = int.TryParse(configuration["Jwt:ExpiresInMinutes"], out var configuredMinutes)
			? configuredMinutes
			: 480;
		var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiresInMinutes);

		var claims = new List<Claim>
		{
			new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
			new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? user.Email ?? string.Empty),
			new(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new(ClaimTypes.Name, user.FullName),
			new(ClaimTypes.Email, user.Email ?? string.Empty)
		};

		claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

		var credentials = new SigningCredentials(
			new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
			SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: issuer,
			audience: audience,
			claims: claims,
			notBefore: DateTime.UtcNow,
			expires: expiresAtUtc,
			signingCredentials: credentials);

		return Task.FromResult(new AuthTokenResult
		{
			AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
			ExpiresAtUtc = expiresAtUtc
		});
	}
}

public sealed class AuthTokenResult
{
	public string AccessToken { get; set; } = string.Empty;
	public DateTime ExpiresAtUtc { get; set; }
}