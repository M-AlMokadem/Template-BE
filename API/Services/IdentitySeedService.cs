using API.Constants;
using Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace API.Services;

public static class IdentitySeedService
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        await EnsureRoleExistsAsync(roleManager, IdentityRoles.Admin, "System administrators");
        await EnsureRoleExistsAsync(roleManager, IdentityRoles.User, "Standard application users");
    }

    private static async Task EnsureRoleExistsAsync(RoleManager<ApplicationRole> roleManager, string roleName, string description)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var role = new ApplicationRole
        {
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
            Description = description,
            IsActive = true
        };

        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(" | ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to seed role '{roleName}'. {errors}");
        }
    }
}
