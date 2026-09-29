using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations;

public sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("ApplicationRoles");

        builder.Property(role => role.Description)
            .HasMaxLength(500);

        builder.HasIndex(role => role.NormalizedName)
            .IsUnique()
            .HasDatabaseName("RoleNameIndex");
    }
}
