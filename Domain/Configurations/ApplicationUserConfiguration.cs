using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("ApplicationUsers");

        builder.Property(user => user.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("EmailIndex");

        builder.HasIndex(user => user.NormalizedUserName)
            .IsUnique()
            .HasDatabaseName("UserNameIndex");
    }
}
