using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations;

public sealed class RefreshTokenConfiguration : BaseEntityConfiguration<RefreshToken>
{
    public override void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        base.Configure(builder);

        builder.ToTable("RefreshTokens");
        builder.Property(token => token.TokenHash).IsRequired().HasMaxLength(128);
        builder.Property(token => token.AccessTokenJti).IsRequired().HasMaxLength(64);
        builder.Property(token => token.ReplacedByTokenHash).HasMaxLength(128);
        builder.Property(token => token.ExpiresAtUtc).IsRequired();

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.AccessTokenJti).IsUnique();
        builder.HasIndex(token => new { token.ApplicationUserId, token.RevokedAtUtc });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(token => token.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
