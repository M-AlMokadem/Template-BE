using Domain.Base;

namespace Domain.Models;

public sealed class RefreshToken : BaseEntity
{
    public Guid ApplicationUserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string AccessTokenJti { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
}
