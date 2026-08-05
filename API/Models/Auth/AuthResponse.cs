namespace API.Models.Auth;

public sealed class AuthResponse
{
	public string AccessToken { get; set; } = string.Empty;
	public DateTime ExpiresAtUtc { get; set; }
	public AuthUserResponse User { get; set; } = new();
}

public sealed class AuthUserResponse
{
	public string Id { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
}