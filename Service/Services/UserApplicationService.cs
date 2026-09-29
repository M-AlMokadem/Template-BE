using Domain.Context;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Service.Constants;
using Service.IServices;
using Service.Models;

namespace Service.Services;

public sealed class UserApplicationService : IUserApplicationService
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 200;

    private readonly ApplicationContext _applicationContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserApplicationService(
        ApplicationContext applicationContext,
        UserManager<ApplicationUser> userManager)
    {
        _applicationContext = applicationContext;
        _userManager = userManager;
    }

    public async Task<PagedUsers> GetUsersAsync(UserListQuery query)
    {
        var usersQuery = _applicationContext.ApplicationUsers
            .AsNoTracking()
            .Where(user => !user.IsDeleted);

        var normalizedSearch = query.SearchValue?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var normalizedColumns = query.Columns
                .Where(column => !string.IsNullOrWhiteSpace(column))
                .Select(column => column.Trim().ToLowerInvariant())
                .ToHashSet();

            var filterByName = normalizedColumns.Count == 0 || normalizedColumns.Contains("fullname");
            var filterByEmail = normalizedColumns.Count == 0 || normalizedColumns.Contains("email");

            usersQuery = usersQuery.Where(user =>
                (filterByName && EF.Functions.ILike(user.FullName, $"%{normalizedSearch}%")) ||
                (filterByEmail && EF.Functions.ILike(user.Email ?? string.Empty, $"%{normalizedSearch}%")));
        }

        var totalCount = await usersQuery.CountAsync();
        var safePageSize = query.PageSize <= 0 ? DefaultPageSize : Math.Min(query.PageSize, MaxPageSize);
        var safePageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;

        usersQuery = usersQuery.OrderBy(user => user.FullName);
        if (query.IsPaginationRequest)
        {
            usersQuery = usersQuery
                .Skip((safePageNumber - 1) * safePageSize)
                .Take(safePageSize);
        }
        else
        {
            safePageNumber = 1;
            safePageSize = Math.Max(totalCount, 1);
        }

        var items = await usersQuery
            .Select(ToSummaryExpression())
            .ToListAsync();

        var totalPages = Math.Max((int)Math.Ceiling(totalCount / (double)Math.Max(safePageSize, 1)), 1);
        return new PagedUsers
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

    public async Task<UserCreationResult> CreateUserAsync(CreateUserCommand command)
    {
        var email = command.Email.Trim();
        var user = new ApplicationUser
        {
            FullName = command.FullName.Trim(),
            UserName = email,
            Email = email,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded)
        {
            return Failure(result.Errors.Select(error => error.Description));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, IdentityRoles.User);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return Failure(roleResult.Errors.Select(error => error.Description));
        }

        return new UserCreationResult
        {
            Succeeded = true,
            User = ToSummary(user)
        };
    }

    public async Task<UserSummary?> GetByIdAsync(Guid id)
    {
        var user = await FindActiveUserAsync(id);
        return user is null ? null : ToSummary(user);
    }

    public async Task<UserSummary?> UpdateStatusAsync(Guid id, bool isActive)
    {
        var user = await FindActiveUserAsync(id);
        if (user is null)
        {
            return null;
        }

        user.IsActive = isActive;
        user.ModifiedOn = DateTime.UtcNow;
        await _applicationContext.SaveChangesAsync();
        return ToSummary(user);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await FindActiveUserAsync(id);
        if (user is null)
        {
            return false;
        }

        user.IsDeleted = true;
        user.IsActive = false;
        user.ModifiedOn = DateTime.UtcNow;
        await _applicationContext.SaveChangesAsync();
        return true;
    }

    private async Task<ApplicationUser?> FindActiveUserAsync(Guid id)
    {
        return await _applicationContext.ApplicationUsers
            .FirstOrDefaultAsync(user => user.Id == id && !user.IsDeleted);
    }

    private static UserSummary ToSummary(ApplicationUser user)
    {
        return new UserSummary
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive
        };
    }

    private static System.Linq.Expressions.Expression<Func<ApplicationUser, UserSummary>> ToSummaryExpression()
    {
        return user => new UserSummary
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive
        };
    }

    private static UserCreationResult Failure(IEnumerable<string> errors)
    {
        return new UserCreationResult
        {
            Succeeded = false,
            Errors = errors.ToArray()
        };
    }
}
