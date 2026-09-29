using API.Constants;
using API.Models.Users;
using Domain.Context;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public sealed class UserService : IUserService
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 200;

    private readonly ApplicationContext _applicationContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(
        ApplicationContext applicationContext,
        UserManager<ApplicationUser> userManager)
    {
        _applicationContext = applicationContext;
        _userManager = userManager;
    }

    public async Task<PagedResult<UserSummaryResponse>> GetUsersAsync(
        string? searchedValue,
        List<string>? columnsKey,
        bool isPaginationRequest,
        int pageSize,
        int pageNumber)
    {
        var query = _applicationContext.ApplicationUsers
            .AsNoTracking()
            .Where(user => !user.IsDeleted);

        var normalizedSearch = searchedValue?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var normalizedColumns = (columnsKey ?? [])
                .Where(column => !string.IsNullOrWhiteSpace(column))
                .Select(column => column.Trim().ToLowerInvariant())
                .ToHashSet();

            var filterByName = normalizedColumns.Count == 0 || normalizedColumns.Contains("fullname");
            var filterByEmail = normalizedColumns.Count == 0 || normalizedColumns.Contains("email");

            query = query.Where(user =>
                (filterByName && EF.Functions.ILike(user.FullName, $"%{normalizedSearch}%")) ||
                (filterByEmail && EF.Functions.ILike(user.Email ?? string.Empty, $"%{normalizedSearch}%")));
        }

        var totalCount = await query.CountAsync();
        var safePageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        var safePageNumber = pageNumber <= 0 ? 1 : pageNumber;

        if (isPaginationRequest)
        {
            query = query
                .OrderBy(user => user.FullName)
                .Skip((safePageNumber - 1) * safePageSize)
                .Take(safePageSize);
        }
        else
        {
            query = query.OrderBy(user => user.FullName);
            safePageNumber = 1;
            safePageSize = Math.Max(totalCount, 1);
        }

        var items = await query
            .Select(user => new UserSummaryResponse
            {
                Id = user.Id.ToString(),
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                IsActive = user.IsActive
            })
            .ToListAsync();

        var totalPages = Math.Max((int)Math.Ceiling(totalCount / (double)Math.Max(safePageSize, 1)), 1);
        return new PagedResult<UserSummaryResponse>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = safePageNumber,
            PageSize = safePageSize,
            TotalPages = totalPages,
            HasNextPage = safePageNumber < totalPages,
            HasPreviousPage = safePageNumber > 1
        };
    }

    public async Task<(ApplicationUser? User, IdentityResult Result)> CreateUserAsync(CreateUserRequest request)
    {
        var email = request.Email.Trim();
        var user = new ApplicationUser
        {
            FullName = request.FullName.Trim(),
            UserName = email,
            Email = email,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return (null, result);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, IdentityRoles.User);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return (null, roleResult);
        }

        return (user, IdentityResult.Success);
    }
}
