using API.Constants;
using Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace API.Services;

public static class IdentitySeedService
{
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        await EnsureRoleExistsAsync(roleManager, IdentityRoles.Admin, "System administrators");
        await EnsureRoleExistsAsync(roleManager, IdentityRoles.User, "Standard application users");

        if (!environment.IsDevelopment())
        {
            return;
        }

        var email = configuration["SeedAdmin:Email"]?.Trim();
        var password = configuration["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                FullName = "Development Administrator",
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(admin, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(" | ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to seed development admin. {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, IdentityRoles.Admin))
        {
            var roleResult = await userManager.AddToRoleAsync(admin, IdentityRoles.Admin);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(" | ", roleResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to assign development admin role. {errors}");
            }
        }
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
