using API.Configuration;
using Domain.Models;
using Microsoft.Extensions.Options;
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
	private readonly JwtOptions options;

	public JwtTokenService(IOptions<JwtOptions> options)
	{
		this.options = options.Value;
	}

	public Task<AuthTokenResult> CreateTokenAsync(ApplicationUser user, IEnumerable<string> roles)
	{
		var issuer = options.Issuer;
		var audience = options.Audience;
		var key = options.Key;
		var expiresInMinutes = options.ExpiresInMinutes;
		var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiresInMinutes);
		var jti = Guid.NewGuid().ToString("N");

		var claims = new List<Claim>
		{
			new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new(JwtRegisteredClaimNames.Jti, jti),
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
			ExpiresAtUtc = expiresAtUtc,
			Jti = jti
		});
	}
}

public sealed class AuthTokenResult
{
	public string AccessToken { get; set; } = string.Empty;
	public DateTime ExpiresAtUtc { get; set; }
	public string Jti { get; set; } = string.Empty;
}